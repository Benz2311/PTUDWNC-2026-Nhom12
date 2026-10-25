export default function RecipeDetailSkeleton() {
  return (
    <article className="recipe-detail recipe-detail-skeleton" aria-busy="true" aria-label="Đang tải công thức...">
      <div className="skeleton-breadcrumb skeleton" style={{ width: '220px', height: '14px', marginBottom: '16px' }} />
      <header className="recipe-detail-header">
        <div className="recipe-detail-badges" style={{ gap: '8px' }}>
          <div className="skeleton" style={{ width: '80px', height: '24px', borderRadius: '999px' }} />
          <div className="skeleton" style={{ width: '60px', height: '24px', borderRadius: '999px' }} />
        </div>
        <div className="skeleton" style={{ width: '70%', height: '40px', margin: '14px 0 10px' }} />
        <div className="skeleton" style={{ width: '90%', height: '18px', marginBottom: '14px' }} />
        <div className="skeleton" style={{ width: '40%', height: '14px', marginBottom: '14px' }} />
        <div className="skeleton" style={{ width: '50%', height: '20px' }} />
      </header>

      <section className="recipe-detail-gallery" aria-hidden="true">
        <div className="recipe-detail-cover skeleton" style={{ aspectRatio: '16 / 8' }} />
        <div className="recipe-detail-thumbnails" style={{ marginTop: '12px' }}>
          <div className="skeleton" style={{ flex: '0 0 90px', height: '68px', borderRadius: '10px' }} />
          <div className="skeleton" style={{ flex: '0 0 90px', height: '68px', borderRadius: '10px' }} />
          <div className="skeleton" style={{ flex: '0 0 90px', height: '68px', borderRadius: '10px' }} />
        </div>
      </section>

      <div className="detail-columns recipe-detail-columns">
        <div>
          <div className="detail-section">
            <div className="skeleton" style={{ width: '120px', height: '24px', marginBottom: '12px' }} />
            <div className="skeleton" style={{ width: '100%', height: '16px', marginBottom: '8px' }} />
            <div className="skeleton" style={{ width: '95%', height: '16px', marginBottom: '8px' }} />
            <div className="skeleton" style={{ width: '80%', height: '16px' }} />
          </div>

          <div className="detail-section" style={{ marginTop: '24px' }}>
            <div className="skeleton" style={{ width: '140px', height: '24px', marginBottom: '16px' }} />
            <div className="skeleton" style={{ width: '100%', height: '36px', marginBottom: '8px' }} />
            <div className="skeleton" style={{ width: '100%', height: '36px', marginBottom: '8px' }} />
            <div className="skeleton" style={{ width: '100%', height: '36px' }} />
          </div>

          <div className="detail-section" style={{ marginTop: '24px' }}>
            <div className="skeleton" style={{ width: '180px', height: '24px', marginBottom: '16px' }} />
            <div className="skeleton" style={{ width: '100%', height: '80px', marginBottom: '12px' }} />
            <div className="skeleton" style={{ width: '100%', height: '80px' }} />
          </div>
        </div>

        <aside className="nutrition-panel">
          <div className="skeleton" style={{ width: '100px', height: '14px', marginBottom: '8px' }} />
          <div className="skeleton" style={{ width: '140px', height: '22px', marginBottom: '16px' }} />
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            {[1, 2, 3, 4, 5, 6].map((i) => (
              <div key={i} className="skeleton" style={{ height: '48px', borderRadius: '8px' }} />
            ))}
          </div>
        </aside>
      </div>
    </article>
  );
}
