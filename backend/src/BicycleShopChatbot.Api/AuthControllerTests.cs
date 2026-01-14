using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BicycleShopChatbot.Api.Controllers;
using Xunit;

namespace BicycleShopChatbot.Api.Tests
{
    public class AuthControllerTests
    {
        [Fact]
        public void AuthController_CanBeCreated()
        {
            var controller = new AuthController();
            Assert.NotNull(controller);
        }
        // TODO: 실제 인증 메서드에 대한 테스트 추가 필요
    }
}
