using Microsoft.AspNetCore.Mvc;

namespace {{ webui_project_name }}.Controllers.Base
{
    public class ErrorController : BaseController
    {
        public ErrorController(ILogger<BaseController> logger) : base(logger)
        {
        }

        /// <summary>
        /// Yetkisiz işlemler
        /// </summary>
        [Route("error/403")]
        public IActionResult Forbidden()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        /// <summary>
        /// İş kuralı / validation
        /// </summary>
        [Route("error/400")]
        public IActionResult InvalidProcess(string? message = default)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(message);
        }

        /// <summary>
        /// Veri veya sayfa bulunamadı
        /// </summary>
        [Route("error/404")]
        public IActionResult NotFoundPage()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View();
        }

        /// <summary>
        /// Exception yakalandığı durumlar
        /// </summary>
        [Route("error/500")]
        public IActionResult InternalServer(string? message = default)
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return View(message);
        }
    }
}
