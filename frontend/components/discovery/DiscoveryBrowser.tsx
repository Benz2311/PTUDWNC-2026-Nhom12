type DiscoveryBrowserProps = {
  mode?: 'explore' | 'search';
};

export default function DiscoveryBrowser({ mode = 'explore' }: DiscoveryBrowserProps) {
  const recipes = [
    'Bún chả Hà Nội',
    'Cá kho tộ',
    'Mì xào hải sản',
    'Bánh tráng trộn',
    'Salad gà nướng',
  ];

  return (
    <div className="flow-panel-grid">
      <section className="flow-panel">
        <h2>{mode === 'search' ? 'Kết quả tìm kiếm' : 'Khám phá'}</h2>
        <ul className="list-plain">
          {recipes.map((recipe) => (
            <li key={recipe}>{recipe}</li>
          ))}
        </ul>
      </section>
      <section className="flow-panel">
        <h2>Danh mục gợi ý</h2>
        <p>Món Việt, Món Âu, Món chay, Tráng miệng, Đồ uống.</p>
      </section>
    </div>
  );
}
