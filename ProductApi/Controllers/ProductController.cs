using Microsoft.AspNetCore.Mvc;

namespace ProductApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly List<Product> _products = new List<Product>
        {
            new Product{Name="Laptop",Price=5000,Stock=100},
            new Product{Name="Phone",Price=2000,Stock=200},
            new Product{Name="Monitor",Price=500,Stock=400},
            new Product{Name="Mouse",Price=50,Stock=1000},
        };

        private readonly ILogger<ProductController> _logger;

        public ProductController(ILogger<ProductController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<Product> Get()
        {
            return _products;
        }
        [HttpPost]
        public IActionResult Add([FromBody] Product product)
        {
            _products.Add(product);
            return Ok("Successfully");
        }
    }
}