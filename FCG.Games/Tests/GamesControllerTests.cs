using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FCG.Games.Controllers;
using FCG.Games.Models;
using FCG.Games.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FCG.Games.Tests
{
    public class GamesControllerTests
    {
        private GamesController CreateController(IGameService games, IPurchaseService purchases)
        {
            var ctrl = new GamesController(games, purchases);
            // add fake authenticated user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            }, "Test"));
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            return ctrl;
        }

        [Fact]
        public async Task Create_Returns_CreatedResult()
        {
            var mockGames = new Mock<IGameService>();
            mockGames.Setup(g => g.CreateAsync(It.IsAny<Game>()))
                     .ReturnsAsync((Game g) => { g.Id = Guid.NewGuid(); return g; });

            var mockPurch = new Mock<IPurchaseService>();
            var ctrl = CreateController(mockGames.Object, mockPurch.Object);

            var input = new Game { Title = "foo" };
            var result = await ctrl.Create(input);
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.NotNull(created.Value);
        }

        [Fact]
        public async Task Purchase_Returns_Accepted_When_User_Valid()
        {
            var gameId = Guid.NewGuid();
            var mockGames = new Mock<IGameService>();
            var mockPurch = new Mock<IPurchaseService>();
            mockPurch.Setup(p => p.CreatePurchaseIntentAsync(It.IsAny<Guid>(), gameId, It.IsAny<string?>()))
                     .ReturnsAsync(new PurchaseOrder { Id = Guid.NewGuid(), Status = PurchaseStatus.PendingPayment });

            var ctrl = CreateController(mockGames.Object, mockPurch.Object);
            ctrl.ControllerContext.HttpContext.Request.Headers["x-correlation-id"] = "cid";

            var res = await ctrl.Purchase(gameId);
            var accepted = Assert.IsType<AcceptedResult>(res);
        }
    }
}