using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlexaController.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlexaController : ControllerBase
    {

        private readonly ILogger<AlexaController> _logger;

        public AlexaController(ILogger<AlexaController> logger)
        {
            _logger = logger;
        }



        [HttpGet]
        public async Task ApagarEquipo()
        {
            throw new NotImplementedException(); 
        }

        [HttpGet]
        public async Task ReiniciarEquipo()
        {
            throw new NotImplementedException();
        }


        [HttpGet]
        public async Task IniciarSteam()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task CerrarSteam()
        {
            throw new NotImplementedException();
        }


        [HttpGet]
        public async Task ReiniciarSteam()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task CerrarRetroArch()
        {
            throw new NotImplementedException();
        }


        [HttpGet]
        public async Task IniciarModoJuegos()
        {
            throw new NotImplementedException();
        }


        [HttpGet]
        public async Task DetenerModoJuegos()
        {
            throw new NotImplementedException();
        }






    }
}

