import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  activateTemplateVersion,
  createAddOn,
  createTemplate,
  createTemplateVersion,
  getAddOns,
  getFeatures,
  getTemplate,
  getTemplates,
  saveTemplateAddOns,
  saveTemplateFeatures,
  setTemplateActive,
  updateAddOn,
  updateTemplate,
  type AddOn,
  type AddOnRequest,
  type TemplateFeatureRequest,
  type TemplateRequest,
} from './templatesApi'

const emptyTemplate: TemplateRequest = {
  name: '',
  description: '',
  previewImageReference: '',
}

const emptyAddOn: AddOnRequest = {
  name: '',
  description: '',
  currentPrice: 0,
  isActive: true,
}

export function TemplatesPage() {
  const [templateId, setTemplateId] = useState<number>()
  const [versionId, setVersionId] = useState<number>()
  const [templateForm, setTemplateForm] = useState<TemplateRequest>(emptyTemplate)
  const [versionNumber, setVersionNumber] = useState('')
  const [packageKey, setPackageKey] = useState('')
  const [featureDraft, setFeatureDraft] = useState<TemplateFeatureRequest[]>([])
  const [addOnIds, setAddOnIds] = useState<number[]>([])
  const [addOnForm, setAddOnForm] = useState<AddOnRequest>(emptyAddOn)
  const [editingAddOnId, setEditingAddOnId] = useState<number>()
  const queryClient = useQueryClient()

  const templatesQuery = useQuery({
    queryKey: ['templates'],
    queryFn: getTemplates,
    retry: false,
  })
  const featuresQuery = useQuery({
    queryKey: ['template-features'],
    queryFn: getFeatures,
    retry: false,
  })
  const addOnsQuery = useQuery({
    queryKey: ['addons'],
    queryFn: getAddOns,
    retry: false,
  })
  const templateQuery = useQuery({
    queryKey: ['template', templateId],
    queryFn: () => getTemplate(templateId!),
    enabled: templateId !== undefined,
    retry: false,
  })

  const selectedTemplate = templateQuery.data
  const selectedVersion = selectedTemplate?.versions.find(
    (version) => version.templateVersionId === versionId,
  )

  useEffect(() => {
    if (!selectedTemplate) {
      return
    }

    setTemplateForm({
      name: selectedTemplate.name,
      description: selectedTemplate.description ?? '',
      previewImageReference: selectedTemplate.previewImageReference ?? '',
    })

    const firstVersion = selectedTemplate.versions[0]
    setVersionId(firstVersion?.templateVersionId)
  }, [selectedTemplate])

  useEffect(() => {
    if (!featuresQuery.data || !selectedVersion) {
      setFeatureDraft([])
      setAddOnIds([])
      return
    }

    setFeatureDraft(featuresQuery.data.map((feature) => {
      const saved = selectedVersion.features.find(
        (item) => item.featureId === feature.featureId,
      )

      return {
        featureId: feature.featureId,
        isEnabled: saved?.isEnabled ?? false,
        maximumCount: saved?.maximumCount ?? null,
        maximumFileSizeMb: saved?.maximumFileSizeMb ?? null,
      }
    }))
    setAddOnIds(selectedVersion.addOns.map((addOn) => addOn.addOnId))
  }, [featuresQuery.data, selectedVersion])

  const templateMutation = useMutation({
    mutationFn: () => templateId
      ? updateTemplate(templateId, templateForm)
      : createTemplate(templateForm),
    onSuccess: async (template) => {
      await queryClient.invalidateQueries({ queryKey: ['templates'] })
      setTemplateId(template.templateId)
    },
  })

  const templateStatusMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      setTemplateActive(id, isActive),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['templates'] }),
  })

  const versionMutation = useMutation({
    mutationFn: () => createTemplateVersion(templateId!, versionNumber, packageKey),
    onSuccess: async (version) => {
      setVersionNumber('')
      setPackageKey('')
      setVersionId(version.templateVersionId)
      await refreshTemplateData()
    },
  })

  const featureMutation = useMutation({
    mutationFn: () => saveTemplateFeatures(versionId!, featureDraft),
    onSuccess: refreshTemplateData,
  })

  const addOnSelectionMutation = useMutation({
    mutationFn: () => saveTemplateAddOns(versionId!, addOnIds),
    onSuccess: refreshTemplateData,
  })

  const activateVersionMutation = useMutation({
    mutationFn: activateTemplateVersion,
    onSuccess: refreshTemplateData,
  })

  const addOnMutation = useMutation({
    mutationFn: () => editingAddOnId
      ? updateAddOn(editingAddOnId, addOnForm)
      : createAddOn(addOnForm),
    onSuccess: async () => {
      setEditingAddOnId(undefined)
      setAddOnForm(emptyAddOn)
      await queryClient.invalidateQueries({ queryKey: ['addons'] })
      await refreshTemplateData()
    },
  })

  async function refreshTemplateData() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['templates'] }),
      queryClient.invalidateQueries({ queryKey: ['template', templateId] }),
      queryClient.invalidateQueries({ queryKey: ['addons'] }),
    ])
  }

  function submitTemplate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    templateMutation.mutate()
  }

  function submitVersion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    versionMutation.mutate()
  }

  function submitAddOn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    addOnMutation.mutate()
  }

  function editAddOn(addOn: AddOn) {
    setEditingAddOnId(addOn.addOnId)
    setAddOnForm({
      name: addOn.name,
      description: addOn.description ?? '',
      currentPrice: addOn.currentPrice,
      isActive: addOn.isActive,
    })
  }

  function toggleAddOn(addOnId: number) {
    setAddOnIds((current) => current.includes(addOnId)
      ? current.filter((id) => id !== addOnId)
      : [...current, addOnId])
  }

  const saveError = templateMutation.error
    ?? versionMutation.error
    ?? featureMutation.error
    ?? addOnSelectionMutation.error
    ?? activateVersionMutation.error
    ?? addOnMutation.error
    ?? templateStatusMutation.error

  return (
    <section className="templates-page" dir="rtl">
      <div className="section-heading">
        <div>
          <p className="page-card__eyebrow">WedLink / الإدارة</p>
          <h1>القوالب والإضافات</h1>
          <p>إدارة بيانات القوالب وإصداراتها ومزاياها وأسعار الإضافات.</p>
        </div>
      </div>

      {saveError && <p className="form-error" role="alert">{saveError.message}</p>}
      {templatesQuery.isError && <p className="form-error" role="alert">{templatesQuery.error.message}</p>}
      {featuresQuery.isError && <p className="form-error" role="alert">{featuresQuery.error.message}</p>}
      {addOnsQuery.isError && <p className="form-error" role="alert">{addOnsQuery.error.message}</p>}

      <section className="catalog-panel">
        <div className="catalog-panel__heading">
          <h2>القوالب</h2>
          <button
            className="secondary-button"
            onClick={() => {
              setTemplateId(undefined)
              setVersionId(undefined)
              setTemplateForm(emptyTemplate)
              templateMutation.reset()
            }}
            type="button"
          >قالب جديد</button>
        </div>

        <div className="template-picker">
          {templatesQuery.data?.items.map((template) => (
            <button
              aria-pressed={template.templateId === templateId}
              className={`template-picker__item${template.templateId === templateId ? ' is-selected' : ''}`}
              key={template.templateId}
              onClick={() => setTemplateId(template.templateId)}
              type="button"
            >
              <span>{template.name}</span>
              <small>{template.isActive ? 'نشط' : 'متوقف'}</small>
            </button>
          ))}
          {templatesQuery.isPending && <p role="status">جارٍ تحميل القوالب...</p>}
          {templatesQuery.data?.items.length === 0 && <p className="empty-state">لا توجد قوالب بعد.</p>}
        </div>

        <form className="catalog-form template-form" onSubmit={submitTemplate}>
          <h3>{templateId ? 'بيانات القالب المحدد' : 'إضافة قالب'}</h3>
          <label htmlFor="template-name">اسم القالب</label>
          <input
            id="template-name"
            maxLength={150}
            onChange={(event) => setTemplateForm({ ...templateForm, name: event.target.value })}
            required
            value={templateForm.name}
          />
          <label htmlFor="template-description">الوصف</label>
          <textarea
            id="template-description"
            maxLength={1000}
            onChange={(event) => setTemplateForm({ ...templateForm, description: event.target.value })}
            rows={2}
            value={templateForm.description ?? ''}
          />
          <label htmlFor="template-preview">رابط صورة المعاينة (اختياري)</label>
          <input
            id="template-preview"
            maxLength={500}
            onChange={(event) => setTemplateForm({ ...templateForm, previewImageReference: event.target.value })}
            type="url"
            value={templateForm.previewImageReference ?? ''}
          />
          <div className="form-actions">
            <button className="primary-button" disabled={templateMutation.isPending} type="submit">
              {templateMutation.isPending ? 'جارٍ الحفظ...' : templateId ? 'حفظ بيانات القالب' : 'إنشاء القالب'}
            </button>
            {templateId && selectedTemplate && (
              <button
                className="secondary-button"
                disabled={templateStatusMutation.isPending}
                onClick={() => templateStatusMutation.mutate({
                  id: templateId,
                  isActive: !selectedTemplate.isActive,
                })}
                type="button"
              >{selectedTemplate.isActive ? 'إيقاف القالب' : 'تفعيل القالب'}</button>
            )}
          </div>
        </form>
      </section>

      {templateId && selectedTemplate && (
        <section className="catalog-panel">
          <h2>إصدارات {selectedTemplate.name}</h2>
          <div className="version-list">
            {selectedTemplate.versions.map((version) => (
              <button
                aria-pressed={version.templateVersionId === versionId}
                className={`version-chip${version.templateVersionId === versionId ? ' is-selected' : ''}`}
                key={version.templateVersionId}
                onClick={() => setVersionId(version.templateVersionId)}
                type="button"
              >
                {version.versionNumber}{version.isActive ? ' — نشط' : ''}
              </button>
            ))}
            {selectedTemplate.versions.length === 0 && <p className="empty-state">أضف أول إصدار للقالب.</p>}
          </div>

          <form className="catalog-form version-form" onSubmit={submitVersion}>
            <h3>إضافة إصدار موثوق</h3>
            <label htmlFor="version-number">رقم الإصدار</label>
            <input
              id="version-number"
              maxLength={30}
              onChange={(event) => setVersionNumber(event.target.value)}
              placeholder="1.0"
              required
              value={versionNumber}
            />
            <label htmlFor="package-key">مفتاح القالب داخل التطبيق</label>
            <input
              id="package-key"
              maxLength={150}
              onChange={(event) => setPackageKey(event.target.value)}
              placeholder="classic-v1"
              required
              value={packageKey}
            />
            <small>يُسجل المفتاح فقط؛ لا يتم رفع أو تشغيل ملفات كود من لوحة الإدارة.</small>
            <button className="primary-button" disabled={versionMutation.isPending} type="submit">
              إضافة الإصدار
            </button>
          </form>
        </section>
      )}

      {selectedVersion && (
        <section className="catalog-panel">
          <div className="catalog-panel__heading">
            <div>
              <h2>إعداد الإصدار {selectedVersion.versionNumber}</h2>
              <p>مفتاح الحزمة: <code>{selectedVersion.packageKey}</code></p>
            </div>
            {!selectedVersion.isActive && (
              <button
                className="primary-button"
                disabled={activateVersionMutation.isPending}
                onClick={() => activateVersionMutation.mutate(selectedVersion.templateVersionId)}
                type="button"
              >تفعيل الإصدار</button>
            )}
          </div>

          <form className="catalog-form" onSubmit={(event) => {
            event.preventDefault()
            featureMutation.mutate()
          }}>
            <h3>مزايا القالب وحدوده</h3>
            {featuresQuery.data?.map((feature) => {
              const setting = featureDraft.find((item) => item.featureId === feature.featureId)
              if (!setting) return null

              return (
                <div className="feature-setting" key={feature.featureId}>
                  <label className="checkbox-label">
                    <input
                      checked={setting.isEnabled}
                      onChange={(event) => setFeatureDraft((items) => items.map((item) =>
                        item.featureId === feature.featureId
                          ? { ...item, isEnabled: event.target.checked }
                          : item))}
                      type="checkbox"
                    />
                    <span>{feature.name}</span>
                  </label>
                  <label>
                    الحد الأقصى للعدد
                    <input
                      min={0}
                      onChange={(event) => setFeatureDraft((items) => items.map((item) =>
                        item.featureId === feature.featureId
                          ? { ...item, maximumCount: event.target.value === '' ? null : Number(event.target.value) }
                          : item))}
                      type="number"
                      value={setting.maximumCount ?? ''}
                    />
                  </label>
                  <label>
                    أقصى حجم ملف (MB)
                    <input
                      min={1}
                      onChange={(event) => setFeatureDraft((items) => items.map((item) =>
                        item.featureId === feature.featureId
                          ? { ...item, maximumFileSizeMb: event.target.value === '' ? null : Number(event.target.value) }
                          : item))}
                      type="number"
                      value={setting.maximumFileSizeMb ?? ''}
                    />
                  </label>
                </div>
              )
            })}
            <button className="primary-button" disabled={featureMutation.isPending} type="submit">حفظ المزايا</button>
          </form>

          <form className="catalog-form assigned-addons" onSubmit={(event) => {
            event.preventDefault()
            addOnSelectionMutation.mutate()
          }}>
            <h3>الإضافات المتاحة لهذا الإصدار</h3>
            {addOnsQuery.data?.filter((addOn) => addOn.isActive).map((addOn) => (
              <label className="checkbox-label" key={addOn.addOnId}>
                <input
                  checked={addOnIds.includes(addOn.addOnId)}
                  onChange={() => toggleAddOn(addOn.addOnId)}
                  type="checkbox"
                />
                <span>{addOn.name} — {addOn.currentPrice} {addOn.currency}</span>
              </label>
            ))}
            {addOnsQuery.data?.every((addOn) => !addOn.isActive) && (
              <p className="empty-state">أضف إضافة وفعّلها لإتاحتها لهذا الإصدار.</p>
            )}
            <button className="primary-button" disabled={addOnSelectionMutation.isPending} type="submit">حفظ الإضافات</button>
          </form>
        </section>
      )}

      <section className="catalog-panel addon-panel">
        <h2>الإضافات والأسعار</h2>
        <form className="catalog-form addon-form" onSubmit={submitAddOn}>
          <h3>{editingAddOnId ? 'تعديل الإضافة' : 'إضافة جديدة'}</h3>
          <label htmlFor="addon-name">اسم الإضافة</label>
          <input
            id="addon-name"
            maxLength={150}
            onChange={(event) => setAddOnForm({ ...addOnForm, name: event.target.value })}
            required
            value={addOnForm.name}
          />
          <label htmlFor="addon-description">الوصف</label>
          <textarea
            id="addon-description"
            maxLength={1000}
            onChange={(event) => setAddOnForm({ ...addOnForm, description: event.target.value })}
            rows={2}
            value={addOnForm.description ?? ''}
          />
          <label htmlFor="addon-price">السعر (جنيه مصري)</label>
          <input
            id="addon-price"
            min={0}
            onChange={(event) => setAddOnForm({ ...addOnForm, currentPrice: Number(event.target.value) })}
            required
            step="0.01"
            type="number"
            value={addOnForm.currentPrice}
          />
          <label className="checkbox-label">
            <input
              checked={addOnForm.isActive}
              onChange={(event) => setAddOnForm({ ...addOnForm, isActive: event.target.checked })}
              type="checkbox"
            />
            <span>إضافة نشطة</span>
          </label>
          <div className="form-actions">
            <button className="primary-button" disabled={addOnMutation.isPending} type="submit">
              {editingAddOnId ? 'حفظ الإضافة' : 'إضافة السعر'}
            </button>
            {editingAddOnId && (
              <button className="secondary-button" onClick={() => {
                setEditingAddOnId(undefined)
                setAddOnForm(emptyAddOn)
              }} type="button">إلغاء</button>
            )}
          </div>
        </form>

        <div className="addon-list">
          {addOnsQuery.data?.map((addOn) => (
            <div className="addon-row" key={addOn.addOnId}>
              <div>
                <strong>{addOn.name}</strong>
                <p>{addOn.currentPrice} {addOn.currency} · {addOn.isActive ? 'نشطة' : 'متوقفة'}</p>
              </div>
              <button className="text-button" onClick={() => editAddOn(addOn)} type="button">تعديل</button>
            </div>
          ))}
          {addOnsQuery.data?.length === 0 && <p className="empty-state">لا توجد إضافات بعد.</p>}
        </div>
      </section>
    </section>
  )
}
