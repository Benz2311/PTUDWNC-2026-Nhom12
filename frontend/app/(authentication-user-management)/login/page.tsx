export default function LoginPage() {
  return <FlowPage kicker="Xác thực tài khoản" title="Đăng nhập" description="Truy cập hồ sơ, công thức đã lưu và những trải nghiệm riêng của bạn." panels={["Email và mật khẩu", "Đăng nhập với Google", "Quên mật khẩu"]} />;
}

function FlowPage({ kicker, title, description, panels }: { kicker: string; title: string; description: string; panels: string[] }) {
  return <><div className="flow-kicker">{kicker}</div><h1 className="flow-title">{title}</h1><p className="flow-description">{description}</p><div className="flow-panel-grid">{panels.map((panel) => <section className="flow-panel" key={panel}><h2>{panel}</h2><p>Giao diện sẽ được hoàn thiện trong bước triển khai chức năng của luồng này.</p><a className="flow-panel-link" href="/">Quay về trang chủ →</a></section>)}</div></>;
}
