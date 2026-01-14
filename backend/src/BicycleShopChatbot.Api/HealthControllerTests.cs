using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BicycleShopChatbot.Api.Controllers;
using Xunit;

namespace BicycleShopChatbot.Api.Tests
{
    public class HealthControllerTests
    {
        [Fact]
        public void HealthController_CanBeCreated()
        {
            var controller = new HealthController();
            Assert.NotNull(controller);
        }
        // TODO: 실제 헬스 체크 메서드에 대한 테스트 추가 필요
    }
}
