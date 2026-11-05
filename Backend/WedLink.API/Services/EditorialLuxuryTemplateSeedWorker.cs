using System.Data;
using Microsoft.EntityFrameworkCore;
using WedLink.DataAccess.Persistence;
using WedLink.DataAccess.Templates;

namespace WedLink.API.Services;

/// <summary>
/// Ensures the code-backed Editorial Luxury renderer has a matching catalog
/// entry in a newly provisioned production database.
/// </summary>
public sealed class EditorialLuxuryTemplateSeedWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<EditorialLuxuryTemplateSeedWorker> logger) : BackgroundService
{
    private const string TemplateName = "Editorial Luxury";
    private const string PackageKey = "editorial-luxury-v1";

    private static readonly (string Key, string Name)[] SupportedFeatures =
    [
        ("gallery", "معرض الصور"),
        ("video", "الفيديو"),
        ("music", "الموسيقى"),
        ("countdown", "العد التنازلي"),
        ("rsvp", "تأكيد الحضور"),
        ("guest-messages", "رسائل التهنئة"),
        ("guest-count", "عدد الحضور")
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SeedIfMissingAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Could not seed the Editorial Luxury catalog entry. Will retry in 30 seconds.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task SeedIfMissingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WedLinkDbContext>();
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);

            if (await db.TemplateVersions.AnyAsync(
                    version => version.PackageKey == PackageKey,
                    cancellationToken))
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var featureKeys = SupportedFeatures.Select(item => item.Key).ToArray();
            var features = await db.Features
                .Where(feature => featureKeys.Contains(feature.Key))
                .ToDictionaryAsync(feature => feature.Key, cancellationToken);

            var missingFeature = SupportedFeatures.FirstOrDefault(item => !features.ContainsKey(item.Key));
            if (missingFeature != default)
            {
                throw new InvalidOperationException(
                    $"Template catalog feature '{missingFeature.Key}' is missing. Apply database migrations first.");
            }

            var template = await db.Templates
                .FirstOrDefaultAsync(item => item.Name == TemplateName, cancellationToken);

            if (template is null)
            {
                template = new Template
                {
                    Name = TemplateName,
                    Description = "دعوة زفاف رقمية فاخرة بأسلوب تحريري سينمائي.",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.Templates.Add(template);
                await db.SaveChangesAsync(cancellationToken);
            }

            var usedVersionNumbers = await db.TemplateVersions
                .Where(version => version.TemplateId == template.TemplateId)
                .Select(version => version.VersionNumber)
                .ToListAsync(cancellationToken);

            var versionNumber = "1.0";
            for (var patchVersion = 1; usedVersionNumbers.Contains(versionNumber); patchVersion++)
            {
                versionNumber = $"1.{patchVersion}";
            }

            var version = new TemplateVersion
            {
                TemplateId = template.TemplateId,
                VersionNumber = versionNumber,
                PackageKey = PackageKey,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.TemplateVersions.Add(version);
            await db.SaveChangesAsync(cancellationToken);

            db.TemplateFeatures.AddRange(SupportedFeatures.Select(item => new TemplateFeature
            {
                TemplateVersionId = version.TemplateVersionId,
                FeatureId = features[item.Key].FeatureId,
                IsEnabled = true
            }));

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Seeded {TemplateName} template version {VersionNumber} ({PackageKey}) into the catalog.",
                TemplateName, versionNumber, PackageKey);
        });
    }
}
