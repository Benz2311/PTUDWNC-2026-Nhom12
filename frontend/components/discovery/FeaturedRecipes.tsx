export default function FeaturedRecipes() {
  const cards = [
    { title: 'Phở bò truyền thống', description: 'Nước dùng đậm vị, thịt bò mềm, bánh phở thơm.', meta: '4.9 ★ · 1.2k lượt xem' },
    { title: 'Bánh xèo miền Trung', description: 'Tốp mỡ giòn, nhân đậu xanh, tôm, giá đỗ.', meta: '4.8 ★ · 890 lượt xem' },
    { title: 'Gỏi cuốn tươi mát', description: 'Món nhắm thanh mát cho ngày hè.', meta: '4.7 ★ · 760 lượt xem' },
  ];

  return (
    <div className="recipe-grid">
      {cards.map((recipe) => (
        <article className="recipe-card" key={recipe.title}>
          <div className="recipe-thumb" aria-hidden="true">🍲</div>
          <div className="recipe-card-body">
            <span className="recipe-tag">Công thức nổi bật</span>
            <h3>{recipe.title}</h3>
            <p>{recipe.description}</p>
            <div className="recipe-meta">{recipe.meta}</div>
          </div>
        </article>
      ))}
    </div>
  );
}
