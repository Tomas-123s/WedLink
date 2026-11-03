using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WedLink.Business.Templates;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Templates;

public sealed class EfTemplateCatalogRepository(WedLinkDbContext dbContext)
    : ITemplateCatalogRepository
{
    public async Task<TemplatePage> SearchTemplatesAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Templates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(template => template.Name.Contains(search));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var templates = await query
            .OrderBy(template => template.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var templateIds = templates.Select(template => template.TemplateId).ToList();
        var versions = await dbContext.TemplateVersions
            .AsNoTracking()
            .Where(version => templateIds.Contains(version.TemplateId))
            .OrderByDescending(version => version.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = templates.Select(template => ToTemplateData(
            template,
            versions.Where(version => version.TemplateId == template.TemplateId).ToList()))
            .ToList();

        return new TemplatePage(items, page, pageSize, totalItems);
    }

    public async Task<TemplateData?> GetTemplateAsync(
        int templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await dbContext.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.TemplateId == templateId,
                cancellationToken);

        if (template is null)
        {
            return null;
        }

        var versions = await dbContext.TemplateVersions
            .AsNoTracking()
            .Where(version => version.TemplateId == templateId)
            .OrderByDescending(version => version.CreatedAt)
            .ToListAsync(cancellationToken);

        var versionDetails = new List<TemplateVersionData>();
        foreach (var version in versions)
        {
            versionDetails.Add(await GetVersionDataAsync(version, cancellationToken));
        }

        return new TemplateData(
            template.TemplateId,
            template.Name,
            template.Description,
            template.PreviewImageReference,
            template.IsActive,
            template.CreatedAt,
            template.UpdatedAt,
            versionDetails);
    }

    public async Task<TemplateData> CreateTemplateAsync(
        TemplateInput input,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        var template = new Template
        {
            Name = input.Name,
            Description = input.Description,
            PreviewImageReference = input.PreviewImageReference,
            IsActive = true,
            CreatedAt = createdAt
        };

        dbContext.Templates.Add(template);
        await SaveChangesAsync(cancellationToken);
        return ToTemplateData(template, []);
    }

    public async Task<TemplateData?> UpdateTemplateAsync(
        int templateId,
        TemplateInput input,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        var template = await dbContext.Templates.FirstOrDefaultAsync(
            item => item.TemplateId == templateId,
            cancellationToken);

        if (template is null)
        {
            return null;
        }

        template.Name = input.Name;
        template.Description = input.Description;
        template.PreviewImageReference = input.PreviewImageReference;
        template.UpdatedAt = updatedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetTemplateAsync(templateId, cancellationToken);
    }

    public async Task<bool> SetTemplateActiveAsync(
        int templateId,
        bool isActive,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        var template = await dbContext.Templates.FirstOrDefaultAsync(
            item => item.TemplateId == templateId,
            cancellationToken);

        if (template is null)
        {
            return false;
        }

        template.IsActive = isActive;
        template.UpdatedAt = updatedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<FeatureData>> GetFeaturesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Features
            .AsNoTracking()
            .OrderBy(feature => feature.Name)
            .Select(feature => new FeatureData(
                feature.FeatureId,
                feature.Key,
                feature.Name,
                feature.Description))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> VersionNumberExistsAsync(
        int templateId,
        string versionNumber,
        CancellationToken cancellationToken = default)
    {
        return dbContext.TemplateVersions.AnyAsync(
            version => version.TemplateId == templateId
                && version.VersionNumber == versionNumber,
            cancellationToken);
    }

    public async Task<TemplateVersionData> CreateVersionAsync(
        int templateId,
        TemplateVersionInput input,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        var version = new TemplateVersion
        {
            TemplateId = templateId,
            VersionNumber = input.VersionNumber,
            PackageKey = input.PackageKey,
            IsActive = false,
            CreatedAt = createdAt
        };

        dbContext.TemplateVersions.Add(version);
        await SaveChangesAsync(cancellationToken);
        return await GetVersionDataAsync(version, cancellationToken);
    }

    public async Task<TemplateVersionData?> GetVersionAsync(
        int templateVersionId,
        CancellationToken cancellationToken = default)
    {
        var version = await dbContext.TemplateVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.TemplateVersionId == templateVersionId,
                cancellationToken);

        return version is null
            ? null
            : await GetVersionDataAsync(version, cancellationToken);
    }

    public async Task<bool> SetVersionFeaturesAsync(
        int templateVersionId,
        IReadOnlyList<TemplateFeatureInput> features,
        CancellationToken cancellationToken = default)
    {
        var versionExists = await dbContext.TemplateVersions.AnyAsync(
            version => version.TemplateVersionId == templateVersionId,
            cancellationToken);

        if (!versionExists)
        {
            return false;
        }

        var currentFeatures = await dbContext.TemplateFeatures
            .Where(item => item.TemplateVersionId == templateVersionId)
            .ToListAsync(cancellationToken);
        dbContext.TemplateFeatures.RemoveRange(currentFeatures);

        foreach (var feature in features)
        {
            dbContext.TemplateFeatures.Add(new TemplateFeature
            {
                TemplateVersionId = templateVersionId,
                FeatureId = feature.FeatureId,
                IsEnabled = feature.IsEnabled,
                MaximumCount = feature.MaximumCount,
                MaximumFileSizeMb = feature.MaximumFileSizeMb
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetVersionAddOnsAsync(
        int templateVersionId,
        IReadOnlyList<int> addOnIds,
        CancellationToken cancellationToken = default)
    {
        var versionExists = await dbContext.TemplateVersions.AnyAsync(
            version => version.TemplateVersionId == templateVersionId,
            cancellationToken);

        if (!versionExists)
        {
            return false;
        }

        var currentAddOns = await dbContext.TemplateAddOns
            .Where(item => item.TemplateVersionId == templateVersionId)
            .ToListAsync(cancellationToken);
        dbContext.TemplateAddOns.RemoveRange(currentAddOns);

        foreach (var addOnId in addOnIds)
        {
            dbContext.TemplateAddOns.Add(new TemplateAddOn
            {
                TemplateVersionId = templateVersionId,
                AddOnId = addOnId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetVersionActiveAsync(
        int templateVersionId,
        CancellationToken cancellationToken = default)
    {
        var targetVersion = await dbContext.TemplateVersions.FirstOrDefaultAsync(
            version => version.TemplateVersionId == templateVersionId,
            cancellationToken);

        if (targetVersion is null)
        {
            return false;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        var activeVersions = await dbContext.TemplateVersions
            .Where(version => version.TemplateId == targetVersion.TemplateId && version.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var version in activeVersions)
        {
            version.IsActive = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        targetVersion.IsActive = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AddOnData>> GetAddOnsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AddOns
            .AsNoTracking()
            .OrderBy(addOn => addOn.Name)
            .Select(addOn => new AddOnData(
                addOn.AddOnId,
                addOn.Name,
                addOn.Description,
                addOn.CurrentPrice,
                addOn.Currency,
                addOn.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<AddOnData> CreateAddOnAsync(
        AddOnInput input,
        CancellationToken cancellationToken = default)
    {
        var addOn = new AddOn
        {
            Name = input.Name,
            Description = input.Description,
            CurrentPrice = input.CurrentPrice,
            Currency = "EGP",
            IsActive = input.IsActive
        };

        dbContext.AddOns.Add(addOn);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToData(addOn);
    }

    public async Task<AddOnData?> UpdateAddOnAsync(
        int addOnId,
        AddOnInput input,
        CancellationToken cancellationToken = default)
    {
        var addOn = await dbContext.AddOns.FirstOrDefaultAsync(
            item => item.AddOnId == addOnId,
            cancellationToken);

        if (addOn is null)
        {
            return null;
        }

        addOn.Name = input.Name;
        addOn.Description = input.Description;
        addOn.CurrentPrice = input.CurrentPrice;
        addOn.IsActive = input.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToData(addOn);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.GetBaseException() is SqlException sqlException
                && sqlException.Number is 2601 or 2627)
        {
            throw new TemplateConflictException(
                "يوجد إصدار بهذا الرقم لهذا القالب بالفعل.");
        }
    }

    private async Task<TemplateVersionData> GetVersionDataAsync(
        TemplateVersion version,
        CancellationToken cancellationToken)
    {
        var features = await (
            from templateFeature in dbContext.TemplateFeatures.AsNoTracking()
            join feature in dbContext.Features.AsNoTracking()
                on templateFeature.FeatureId equals feature.FeatureId
            where templateFeature.TemplateVersionId == version.TemplateVersionId
            orderby feature.Name
            select new TemplateFeatureData(
                feature.FeatureId,
                feature.Key,
                feature.Name,
                templateFeature.IsEnabled,
                templateFeature.MaximumCount,
                templateFeature.MaximumFileSizeMb))
            .ToListAsync(cancellationToken);

        var addOns = await (
            from templateAddOn in dbContext.TemplateAddOns.AsNoTracking()
            join addOn in dbContext.AddOns.AsNoTracking()
                on templateAddOn.AddOnId equals addOn.AddOnId
            where templateAddOn.TemplateVersionId == version.TemplateVersionId
            orderby addOn.Name
            select new AddOnData(
                addOn.AddOnId,
                addOn.Name,
                addOn.Description,
                addOn.CurrentPrice,
                addOn.Currency,
                addOn.IsActive))
            .ToListAsync(cancellationToken);

        return new TemplateVersionData(
            version.TemplateVersionId,
            version.TemplateId,
            version.VersionNumber,
            version.PackageKey,
            version.IsActive,
            version.CreatedAt,
            features,
            addOns);
    }

    private static TemplateData ToTemplateData(
        Template template,
        IReadOnlyList<TemplateVersion> versions)
    {
        return new TemplateData(
            template.TemplateId,
            template.Name,
            template.Description,
            template.PreviewImageReference,
            template.IsActive,
            template.CreatedAt,
            template.UpdatedAt,
            versions.Select(version => new TemplateVersionData(
                version.TemplateVersionId,
                version.TemplateId,
                version.VersionNumber,
                version.PackageKey,
                version.IsActive,
                version.CreatedAt,
                [],
                [])).ToList());
    }

    private static AddOnData ToData(AddOn addOn)
    {
        return new AddOnData(
            addOn.AddOnId,
            addOn.Name,
            addOn.Description,
            addOn.CurrentPrice,
            addOn.Currency,
            addOn.IsActive);
    }
}
