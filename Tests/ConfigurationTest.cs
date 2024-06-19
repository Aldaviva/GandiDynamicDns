using GandiDynamicDns;

namespace Tests;

public class ConfigurationTest {

    [Fact]
    public void fix() {
        Configuration conf = new() { subdomain = "www.", domain = "example.com", gandiApiKey = "abcdef" };
        conf.fix();
        conf.subdomain.Should().Be("www");

        conf.subdomain = ".";
        conf.fix();
        conf.subdomain.Should().Be("@");

        conf.subdomain = string.Empty;
        conf.fix();
        conf.subdomain.Should().Be("@");

        conf.subdomain = "www";
        conf.fix();
        conf.subdomain.Should().Be("www");
    }

}