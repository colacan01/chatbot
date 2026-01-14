using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BicycleShopChatbot.Api.Controllers;
using Xunit;

namespace BicycleShopChatbot.Api.Tests
{
    public class WeatherForecastControllerTests
    {
        [Fact]
        public void WeatherForecastController_CanBeCreated()
        {
            var controller = new WeatherForecastController();
            Assert.NotNull(controller);
        }
        // TODO: 실제 날씨 예보 메서드에 대한 테스트 추가 필요
    }
}
