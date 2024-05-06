using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace YourNamespace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly string _connectionString = "your_connection_string_here";
        
        [HttpGet("{idProduct:int}/{idWarehouse:int}")]
        public IActionResult CheckProductAndWarehouse(int idProduct, int idWarehouse)
        {
            try
            {
                if (!ProductExists(idProduct))
                {
                    return NotFound("Product with given Id does not exist.");
                }
                
                if (!WarehouseExists(idWarehouse))
                {
                    return NotFound("Warehouse with given Id does not exist.");
                }

                return Ok("Product and warehouse exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult AddProductToWarehouse([FromBody] toAdd request)
        {
            try
            {
                if (!ProductExists(request.IdProduct))
                {
                    return BadRequest("Product with given Id does not exist.");
                }
                
                if (!WarehouseExists(request.IdWarehouse))
                {
                    return BadRequest("Warehouse with given Id does not exist.");
                }
                
                if (!OrderExists(request.IdProduct, request.Amount))
                {
                    return BadRequest("Order for the product does not exist or the amount is incorrect.");
                }
                
                if (OrderFulfilled(request.IdProduct))
                {
                    return BadRequest("The order for the product has already been fulfilled.");
                }
                
                UpdateOrderFulfilled(request.IdProduct);
                
                int insertedId = InsertProductWarehouse(request);

                return Ok(insertedId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private bool ProductExists(int idProduct)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", idProduct);
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        private bool WarehouseExists(int idWarehouse)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Warehouses WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", idWarehouse);
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        private bool OrderExists(int idProduct, int amount)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Orders WHERE IdProduct = @IdProduct AND Amount = @Amount", connection);
                command.Parameters.AddWithValue("@IdProduct", idProduct);
                command.Parameters.AddWithValue("@Amount", amount);
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        private bool OrderFulfilled(int idProduct)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Product_Warehouse WHERE IdProduct = @IdProduct", connection);
                command.Parameters.AddWithValue("@IdProduct", idProduct);
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        private void UpdateOrderFulfilled(int idProduct)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("UPDATE Orders SET FulfilledAt = GETDATE() WHERE IdProduct = @IdProduct", connection);
                command.Parameters.AddWithValue("@IdProduct", idProduct);
                command.ExecuteNonQuery();
            }
        }

        private int InsertProductWarehouse(toAdd request)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("INSERT INTO Product_Warehouse (IdProduct, IdWarehouse, Price, CreatedAt) VALUES (@IdProduct, @IdWarehouse, @Price, GETDATE()); SELECT SCOPE_IDENTITY();", connection);
                command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
                command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
                
                command.Parameters.AddWithValue("@Price", GetProductPrice(request.IdProduct) * request.Amount);

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private decimal GetProductPrice(int idProduct)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT Price FROM Products WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", idProduct);
                return Convert.ToDecimal(command.ExecuteScalar());
            }
        }
    }

    public class toAdd
    {
        public int IdWarehouse { get; set; }
        public int IdProduct { get; set; }
        public int Amount { get; set; }
    }
}
