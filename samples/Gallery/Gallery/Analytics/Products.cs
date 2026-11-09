namespace Gallery.Analytics;

/// <summary>What a product is measured by: users of a web app, of a mobile app, or calls to an API.</summary>
public enum ProductKind
{
    Web,
    Mobile,
    Api,
}

/// <summary>A named share of a product's traffic: a page, a source, a device or a region, with its weight.</summary>
public sealed record Share(string Name, double Weight);

/// <summary>A kind of event the live feed shows, with how often it happens among the product's events.</summary>
public sealed record EventKind(string Title, string Tone, double Weight, double Amount = 0);

/// <summary>
/// One of the dashboard's dummy products: its name and pitch, the size and daily shape of its traffic, and the shares
/// its pages, sources, devices and funnel split it into. Every number the dashboard shows derives from these and the
/// seeded generator.
/// </summary>
public sealed record Product(string Id, string Name, ProductKind Kind, string Pitch)
{
    /// <summary>Active users (or requests per second for an API) at the busiest hour of a weekday.</summary>
    public double Peak { get; init; }

    /// <summary>The daily shape: a floor and bumps of activity, each an hour, a width in hours and a height.</summary>
    public IReadOnlyList<(double Hour, double Width, double Height)> Bumps { get; init; } = [];

    /// <summary>The share of the peak the product keeps at its quietest hour.</summary>
    public double Floor { get; init; }

    /// <summary>The traffic of a weekend day against a weekday.</summary>
    public double Weekend { get; init; } = 1;

    /// <summary>The second series of the main chart against the first (page views per visitor, sessions per user).</summary>
    public double SecondaryRatio { get; init; } = 2;

    public string PrimaryName { get; init; } = "Visitors";

    public string SecondaryName { get; init; } = "Page views";

    public string PagesTitle { get; init; } = "Top pages";

    public string SourcesTitle { get; init; } = "Traffic sources";

    public string DevicesTitle { get; init; } = "Devices";

    public string FunnelTitle { get; init; } = "Conversion funnel";

    public IReadOnlyList<Share> Pages { get; init; } = [];

    public IReadOnlyList<Share> Sources { get; init; } = [];

    public IReadOnlyList<Share> Devices { get; init; } = [];

    /// <summary>The funnel's steps, each the share of the first that reaches it.</summary>
    public IReadOnlyList<Share> Funnel { get; init; } = [];

    public IReadOnlyList<EventKind> Events { get; init; } = [];

    /// <summary>Events per second at the peak.</summary>
    public double EventRate { get; init; } = 1.5;

    public IReadOnlyList<string> Places { get; init; } = Cities;

    /// <summary>The conversion the funnel's last step reaches from the first, in percent.</summary>
    public double Conversion => Funnel.Count > 0 ? Funnel[^1].Weight * 100 : 0;

    private static readonly string[] Cities =
    [
        "Berlin, DE", "Lisbon, PT", "Austin, US", "Toronto, CA", "Paris, FR", "Bangalore, IN", "Tokyo, JP", "São Paulo, BR",
        "London, GB", "Sydney, AU", "Seoul, KR", "Amsterdam, NL", "Madrid, ES", "Nairobi, KE", "Warsaw, PL", "Denver, US",
    ];
}

/// <summary>The three dummy products the dashboard follows.</summary>
public static class Products
{
    public static Product Web { get; } = new("web", "Nimbus", ProductKind.Web, "Team workspace · SaaS web app")
    {
        Peak = 1840,
        Floor = 0.16,
        Bumps = [(10.5, 2.4, 0.78), (15.2, 2.6, 0.7), (21, 1.6, 0.18)],
        Weekend = 0.48,
        SecondaryRatio = 2.6,
        Pages = [new("/", 0.31), new("/pricing", 0.17), new("/docs/getting-started", 0.14), new("/blog/launch-week", 0.11), new("/changelog", 0.08), new("/signup", 0.06)],
        Sources = [new("Search", 0.41), new("Direct", 0.27), new("Referral", 0.14), new("Social", 0.11), new("Email", 0.07)],
        Devices = [new("Desktop", 0.68), new("Mobile", 0.27), new("Tablet", 0.05)],
        Funnel = [new("Visited", 1), new("Engaged", 0.56), new("Signed up", 0.24), new("Subscribed", 0.046)],
        Events =
        [
            new("New signup", "signup", 0.34), new("Upgraded to Pro", "purchase", 0.12, 49), new("Invited teammates", "info", 0.22),
            new("Bought Team plan", "purchase", 0.05, 249), new("Created a workspace", "info", 0.22), new("Payment failed", "error", 0.05),
        ],
        EventRate = 1.6,
    };

    public static Product Mobile { get; } = new("mobile", "Pulse", ProductKind.Mobile, "Fitness tracker · iOS and Android")
    {
        Peak = 4200,
        Floor = 0.12,
        Bumps = [(7.2, 1.3, 0.82), (12.6, 1.2, 0.28), (19.4, 1.9, 0.92)],
        Weekend = 1.18,
        PrimaryName = "Active users",
        SecondaryName = "Sessions",
        SecondaryRatio = 1.7,
        PagesTitle = "Top screens",
        SourcesTitle = "Install sources",
        DevicesTitle = "Platforms",
        Pages = [new("Today", 0.29), new("Workout", 0.21), new("Run tracker", 0.16), new("Progress", 0.13), new("Coach chat", 0.09), new("Settings", 0.04)],
        Sources = [new("App Store", 0.38), new("Google Play", 0.29), new("Invites", 0.15), new("Paid social", 0.12), new("Web", 0.06)],
        Devices = [new("iOS", 0.58), new("Android", 0.39), new("Watch", 0.03)],
        Funnel = [new("Installed", 1), new("Onboarded", 0.71), new("Started trial", 0.29), new("Subscribed", 0.086)],
        Events =
        [
            new("Finished a workout", "info", 0.36), new("Started a trial", "signup", 0.2), new("Subscribed yearly", "purchase", 0.06, 59.99),
            new("Subscribed monthly", "purchase", 0.08, 9.99), new("Hit a streak", "info", 0.24), new("Crash reported", "error", 0.06),
        ],
        EventRate = 2.2,
    };

    public static Product Api { get; } = new("api", "Relay", ProductKind.Api, "Payments API · REST and webhooks")
    {
        Peak = 3400,
        Floor = 0.42,
        Bumps = [(14, 4.2, 0.58), (2, 1.4, 0.12)],
        Weekend = 0.78,
        PrimaryName = "Requests",
        SecondaryName = "Cache hits",
        SecondaryRatio = 0.62,
        PagesTitle = "Top endpoints",
        SourcesTitle = "Client SDKs",
        DevicesTitle = "Regions",
        FunnelTitle = "Payment pipeline",
        Pages = [new("POST /v1/charges", 0.27), new("GET /v1/customers", 0.22), new("GET /v1/balance", 0.16), new("POST /v1/refunds", 0.09), new("GET /v1/events", 0.08), new("POST /v1/webhooks", 0.06)],
        Sources = [new("Node", 0.36), new("Python", 0.28), new("Go", 0.16), new("Java", 0.12), new("cURL", 0.08)],
        Devices = [new("us-east", 0.46), new("eu-west", 0.34), new("ap-south", 0.2)],
        Funnel = [new("Created", 1), new("Authorized", 0.93), new("Captured", 0.88), new("Settled", 0.86)],
        Events =
        [
            new("Charge captured", "purchase", 0.4, 120), new("Refund issued", "info", 0.12, 35), new("Webhook delivered", "info", 0.28),
            new("New API key", "signup", 0.08), new("Rate limited", "error", 0.07), new("Card declined", "error", 0.05),
        ],
        EventRate = 2.6,
    };

    public static IReadOnlyList<Product> All { get; } = [Web, Mobile, Api];

    /// <summary>The product with this id, or the first one for an unknown or missing id.</summary>
    public static Product Find(string? id) => All.FirstOrDefault(product => string.Equals(product.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Web;
}
