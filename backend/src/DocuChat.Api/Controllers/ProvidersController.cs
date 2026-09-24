using DocuChat.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuChat.Api.Controllers;

[ApiController, Authorize, Route("api/providers")]
public sealed class ProvidersController(IProviderCatalog catalog) : ControllerBase
{
    [HttpGet]
    public ActionResult<ProviderCatalogDto> Get() => Ok(catalog.Get());
}
