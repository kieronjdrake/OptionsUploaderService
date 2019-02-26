using FluentAssertions;
using LanguageExt.UnitTesting;
using Xunit;

namespace Prax.Aspect.Test
{
    public class TestOptionType {
        [Fact]
        public void FromContractTypeStringShouldProduceTheCorrectOptionType() {
            OptionTypeHelpers.FromContractTypeString("p").ShouldBeSome(x => x.Should().Be(OptionType.Put));
            OptionTypeHelpers.FromContractTypeString("P").ShouldBeSome(x => x.Should().Be(OptionType.Put));
            OptionTypeHelpers.FromContractTypeString("put").ShouldBeSome(x => x.Should().Be(OptionType.Put));
            OptionTypeHelpers.FromContractTypeString("PUT").ShouldBeSome(x => x.Should().Be(OptionType.Put));
            OptionTypeHelpers.FromContractTypeString("Put").ShouldBeSome(x => x.Should().Be(OptionType.Put));

            OptionTypeHelpers.FromContractTypeString("c").ShouldBeSome(x => x.Should().Be(OptionType.Call));
            OptionTypeHelpers.FromContractTypeString("C").ShouldBeSome(x => x.Should().Be(OptionType.Call));
            OptionTypeHelpers.FromContractTypeString("call").ShouldBeSome(x => x.Should().Be(OptionType.Call));
            OptionTypeHelpers.FromContractTypeString("CALL").ShouldBeSome(x => x.Should().Be(OptionType.Call));
            OptionTypeHelpers.FromContractTypeString("Call").ShouldBeSome(x => x.Should().Be(OptionType.Call));

            OptionTypeHelpers.FromContractTypeString("bob").ShouldBeNone();
            OptionTypeHelpers.FromContractTypeString("").ShouldBeNone();
            OptionTypeHelpers.FromContractTypeString("failwhale").ShouldBeNone();
        }
    }
}
