using RestProxy.Net.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace RestProxy.Net.Controllers
{
    [Route("api/test")]
    public class TestController : ApiController
    {
        [HttpGet]
        public IEnumerable<DemoItem> GetAll()
        {
            return new List<DemoItem>() {
                    new DemoItem() { Id=1, IsActive=false, Name="1"},
                    new DemoItem() { Id=2, IsActive=false, Name="2"},
            };
        }
    }
}
