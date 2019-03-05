using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DC_CrossLoad_Service.Test
{
    public sealed class UnitTestUtilities
    {
        [Fact]
        public void TestGetConfigItemAsIntSuccess()
        {
            int value = 123;
            Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
            configurationMock.SetupGet(p => p[It.IsAny<string>()]).Returns(value.ToString);

            int ret = Program.GetConfigItemAsInt(configurationMock.Object, "ConfigItem", 321);

            ret.Should().Be(value);
        }

        [Fact]
        public void TestGetConfigItemAsIntFail()
        {
            string value = "String";
            int fail = 321;
            Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
            configurationMock.SetupGet(p => p[It.IsAny<string>()]).Returns(value);

            int ret = Program.GetConfigItemAsInt(configurationMock.Object, "ConfigItem", 321);

            ret.Should().Be(fail);
        }
    }
}
