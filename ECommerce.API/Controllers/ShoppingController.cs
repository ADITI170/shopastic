using ECommerce.API.DataAccess;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ECommerce.API.Models;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingController : ControllerBase
    {
        readonly IDataAccess dataAccess;
        private readonly string DateFormat;
        public ShoppingController(IDataAccess dataAccess, IConfiguration configuration)
        {
            this.dataAccess = dataAccess;
            DateFormat = configuration["Constants:DateFormat"];
        }

        [HttpGet("GetCategoryList")]
        public IActionResult GetCategoryList()
        {
            var result = dataAccess.GetProductCategories();
            return Ok(result);
        }

        [HttpGet("GetProducts")]
        public IActionResult GetProducts(string category, string subcategory, int count)
        {
            var result = dataAccess.GetProducts(category, subcategory, count);
            return Ok(result);
        }

        [HttpGet("GetAllProducts")]
        public IActionResult GetAllProducts()
        {
            var result = dataAccess.GetAllProducts();
            return Ok(result);
        }
        

        [HttpGet("GetProduct/{id}")]
        public IActionResult GetProduct(int id)
        {
            var result = dataAccess.GetProduct(id);
            return Ok(result);
        }

        [HttpPost("RegisterUser")]
        public IActionResult RegisterUser([FromBody] User user)
        {
            user.CreatedAt = DateTime.Now.ToString(DateFormat);
            user.ModifiedAt = DateTime.Now.ToString(DateFormat);

            var result = dataAccess.InsertUser(user);

            string? message;
            if (result) message = "Successfully Registered";
            else message = "email not available";
            return Ok(message);
        }

        [HttpPost("LoginUser")]
        public IActionResult LoginUser([FromBody] User user)
        {
            TokenResponse response = dataAccess.IsUserPresent(user.Email, user.Password);

            //response = string.IsNullOrEmpty(response) ? "invalid" : response;

            return Ok(response);
        }
        [HttpPost("InsertReview")]
        public IActionResult InsertReview([FromBody] Review review)
        {
            review.CreatedAt = DateTime.Now.ToString(DateFormat);
            dataAccess.InsertReview(review);
            return Ok("inserted");
        }

        [HttpGet("GetProductReviews/{productId}")]
        public IActionResult GetProductReviews(int productId)
        {
            var result = dataAccess.GetProductReviews(productId);
            return Ok(result);
        }

        [HttpPost("InsertCartItem/{userid}/{productid}")]
        public IActionResult InsertCartItem(int userid, int productid)
        {
            var result = dataAccess.InsertCartItem(userid, productid);
            return Ok(result ? "inserted" : "not inserted");
        }

        [HttpGet("GetActiveCartOfUser/{id}")]
        public IActionResult GetActiveCartOfUser(int id)
        {
            var result = dataAccess.GetActiveCartOfUser(id);
            return Ok(result);
        }
        [HttpDelete("DeleteCartItem/{userId}/{productId}")]
        public IActionResult DeleteCartItem(int userId, int productId)
        {
            var result = dataAccess.DeleteCartItem(userId, productId);
            if (result)
            {
                return Ok("{\"message\": \"Cart item deleted\"}");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpDelete("DeletePreviousCart/{userId}/{cartId}")]
        public IActionResult DeletePreviousCart(int userId, int cartId)
        {
            var result = dataAccess.DeletePreviousCart(userId, cartId);
            if (result)
            {
                return Ok("{\"message\": \"Cart deleted\"}");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("GetAllPreviousCartsOfUser/{id}")]
        public IActionResult GetAllPreviousCartsOfUser(int id)
        {
            var result = dataAccess.GetAllPreviousCartsOfUser(id);
            return Ok(result);
        }

        [HttpGet("GetPaymentMethods")]
        public IActionResult GetPaymentMethods()
        {
            var result = dataAccess.GetPaymentMethods();
            return Ok(result);
        }

        [HttpPost("InsertPayment")]
        public IActionResult InsertPayment(Payment payment)
        {
            payment.CreatedAt = DateTime.Now.ToString();
            var id = dataAccess.InsertPayment(payment);
            return Ok(id.ToString());
        }

        [HttpPost("InsertOrder")]
        public IActionResult InsertOrder(Order order)
        {
            order.CreatedAt = DateTime.Now.ToString();
            var id = dataAccess.InsertOrder(order);
            return Ok(id.ToString());
        }
        [HttpDelete("DeleteUser/{id}")]
        public IActionResult DeleteUser(int id)
        {
            var result = dataAccess.DeleteUser(id);
            if (result)
            {
                return Ok("{\"message\": \"User deleted\"}");
            }
            else
            {
                return NotFound();
            }
        }
        
        [HttpGet("SearchProducts")]
        public IActionResult SearchProducts([FromQuery] string q)
        {
            var result = dataAccess.SearchProducts(q);
            return Ok(result);
        }
        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers()
        {
            var result = dataAccess.GetAllUsers();
            return Ok(result);
        }
        [HttpGet("GetProductsByCategory")]
        public IActionResult GetProductsByCategory(string category)
        {
            var result = dataAccess.GetProductsByCategory(category);
            return Ok(result);
        }

        [HttpPost("AddProduct")]
        public IActionResult AddProduct([FromBody] Product product)
        {
            var result = dataAccess.InsertProduct(product);
            return Ok(result ? "Product added" : "Failed to add product");
        }

        [HttpPut("UpdateProduct/{id}")]
        public IActionResult UpdateProduct(int id, [FromBody] Product product)
        {
            product.Id = id;
            var result = dataAccess.UpdateProduct(product);
            if (result)
            {
                return Ok(new { message = "Product updated" }); // Return a JSON response with a message property
            }
            else
            {
                return NotFound();
            }
        }
        [HttpDelete("DeleteProduct/{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var result = dataAccess.DeleteProduct(id);
            if (result)
            {
                return Ok("{\"message\": \"Product deleted\"}");
            }
            else
            {
                return NotFound();
            }
        }

    }
}
