namespace Gallery.Landing;

/// <summary>The landing page's text in one language.</summary>
public sealed record LandingCopy(
    string Eyebrow, string Title, string Lead, string Start, string Compare,
    string ChartsLabel, string FasterLabel, string VersionLabel, string MeasuredTitle, string Measured,
    string FamiliesTitle, string FamiliesLead, string CodeTitle, string CodeLead, string Ecosystem, string Gallery,
    string Line, string Area, string Bar, string Composed, string Scatter, string Pie, string Radar, string Radial, string Funnel, string Treemap, string Sankey, string Sunburst)
{
    public static LandingCopy For(string slug) => slug switch
    {
        "es" => Spanish,
        "zh" => Chinese,
        "ja" => Japanese,
        _ => English,
    };

    private static readonly LandingCopy English = new(
        "Aioros.Pictor · Recharts in XAML", "Recharts' charts, with no JavaScript of yours",
        "Recharts 2.15.4's whole API in XAML: written by the server as SVG, interactive through CSS and the framework's script, live when the server pushes new data, and drawn natively on the desktop.",
        "Explore the gallery", "Compared with Recharts",
        "chart types, every Recharts export", "faster than Recharts' server render", "Recharts version whose API it follows", "How it is measured",
        "Every Recharts example is rendered by Recharts and by Pictor and compared; the server timing writes the same examples on the lab's runner, Pictor's SVG against Recharts' renderToStaticMarkup, on the same four cores (docs/benchmarks.md, iteration 12).",
        "Twelve families, live", "Each chart below is XAML this server rendered; hover it, then open its family for every example.",
        "The XAML", "The same markup is a chart on the web and a native control on the desktop.", "Part of Aioros", "The gallery",
        "Line", "Area", "Bar", "Composed", "Scatter", "Pie", "Radar", "Radial bar", "Funnel", "Treemap", "Sankey", "Sunburst");

    private static readonly LandingCopy Spanish = new(
        "Aioros.Pictor · Recharts en XAML", "Las gráficas de Recharts, sin JavaScript tuyo",
        "Toda la API de Recharts 2.15.4 en XAML: el servidor la escribe como SVG, es interactiva con CSS y el script del framework, se actualiza en vivo cuando el servidor envía datos nuevos y se dibuja nativa en el escritorio.",
        "Explorar la galería", "Comparado con Recharts",
        "tipos de gráfica, cada export de Recharts", "más rápido que el render de servidor de Recharts", "versión de Recharts cuya API sigue", "Cómo se mide",
        "Cada ejemplo de Recharts se renderiza con Recharts y con Pictor y se compara; la medición de servidor escribe los mismos ejemplos en el runner del laboratorio, el SVG de Pictor contra renderToStaticMarkup de Recharts, en los mismos cuatro núcleos (docs/benchmarks.md, iteración 12).",
        "Doce familias, en vivo", "Cada gráfica de abajo es XAML que renderizó este servidor; pasa el cursor y abre su familia para ver todos los ejemplos.",
        "El XAML", "El mismo marcado es una gráfica en la web y un control nativo en el escritorio.", "Parte de Aioros", "La galería",
        "Líneas", "Áreas", "Barras", "Compuesta", "Dispersión", "Pastel", "Radar", "Barras radiales", "Embudo", "Treemap", "Sankey", "Sunburst");

    private static readonly LandingCopy Chinese = new(
        "Aioros.Pictor · XAML 版 Recharts", "Recharts 的图表，无需你写 JavaScript",
        "以 XAML 实现 Recharts 2.15.4 的完整 API：由服务器写成 SVG，借助 CSS 和框架脚本实现交互，服务器推送新数据时实时更新，在桌面端原生绘制。",
        "浏览图库", "与 Recharts 对比",
        "种图表类型，覆盖 Recharts 的全部导出", "比 Recharts 的服务端渲染更快", "所遵循 API 的 Recharts 版本", "如何测量",
        "每个 Recharts 示例都分别由 Recharts 和 Pictor 渲染并比较；服务端计时在实验室的运行器上写出相同示例，以 Pictor 的 SVG 对比 Recharts 的 renderToStaticMarkup，两者使用相同的四个核心（docs/benchmarks.md 第 12 轮）。",
        "十二个图表家族，实时渲染", "下面每个图表都是本服务器渲染的 XAML；悬停查看，再打开其家族页面浏览全部示例。",
        "XAML 代码", "同一份标记在 Web 上是图表，在桌面端是原生控件。", "Aioros 生态的一部分", "图库",
        "折线图", "面积图", "柱状图", "组合图", "散点图", "饼图", "雷达图", "径向柱状图", "漏斗图", "矩形树图", "桑基图", "旭日图");

    private static readonly LandingCopy Japanese = new(
        "Aioros.Pictor · XAML の Recharts", "Recharts のグラフを、JavaScript を書かずに",
        "Recharts 2.15.4 の API 全体を XAML で。サーバーが SVG として書き出し、CSS とフレームワークのスクリプトで操作でき、サーバーが新しいデータを送るとライブで更新され、デスクトップではネイティブに描画されます。",
        "ギャラリーを見る", "Recharts との比較",
        "種類のグラフ、Recharts のすべてのエクスポート", "Recharts のサーバーレンダリングより高速", "API を踏襲する Recharts のバージョン", "測定方法",
        "Recharts の各サンプルを Recharts と Pictor の両方でレンダリングして比較しています。サーバー計測はラボのランナーで同じサンプルを書き出し、Pictor の SVG と Recharts の renderToStaticMarkup を同じ 4 コアで比べたものです（docs/benchmarks.md、第 12 回）。",
        "12 のファミリーをライブで", "以下のグラフはすべてこのサーバーがレンダリングした XAML です。カーソルを合わせ、ファミリーを開くと全サンプルが見られます。",
        "XAML", "同じマークアップが Web ではグラフに、デスクトップではネイティブコントロールになります。", "Aioros の一部", "ギャラリー",
        "折れ線", "エリア", "棒", "複合", "散布図", "円", "レーダー", "放射状の棒", "ファネル", "ツリーマップ", "サンキー", "サンバースト");
}
