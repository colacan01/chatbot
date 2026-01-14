using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BicycleShopChatbot.Api.Controllers;
using Xunit;

namespace BicycleShopChatbot.Api.Tests
{
    public class ChatControllerTests
    {
        [Fact]
        public void ChatController_CanBeCreated()
        {
            var controller = new ChatController();
            Assert.NotNull(controller);
        }
        // TODO: 실제 채팅 메서드에 대한 테스트 추가 필요
    }
}
