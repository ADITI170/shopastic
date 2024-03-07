using ECommerce.API.Models;
using Microsoft.IdentityModel.Tokens;
using System.Data.Common;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using ECommerce.API.Models;

namespace ECommerce.API.DataAccess
{
    public class DataAccess : IDataAccess
    {
        private readonly IConfiguration configuration;
        private readonly string dbconnection;
        private readonly string dateformat;
        public DataAccess(IConfiguration configuration)
        {
            this.configuration = configuration;
            dbconnection = this.configuration["ConnectionStrings:DB"];
            dateformat = this.configuration["Constants:DateFormat"];
        }

        public bool DeleteCartItem(int userId, int productId)
        {
            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };

                connection.Open();

                // Check if the current user has an active cart
                string query = "SELECT COUNT(*) FROM Carts WHERE UserId=@userId AND Ordered='false';";
                command.CommandText = query;
                command.Parameters.AddWithValue("@userId", userId);
                int count = (int)command.ExecuteScalar();
                if (count == 0)
                {
                    return false; // User does not have an active cart
                }

                // Delete the product from the current user's cart
                query = "DELETE TOP(1) FROM CartItems WHERE CartId IN (SELECT CartId FROM Carts WHERE UserId=@userId AND Ordered='false') AND ProductId=@productId;";
                command.CommandText = query;
                command.Parameters.AddWithValue("@productId", productId);
                int rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0; // Return true if at least one row was affected (cart item deleted)
            }
        }

        public bool DeletePreviousCart(int userId, int cartId)
        {
            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };

                connection.Open();
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@cartId", cartId);
                string delete_from_orders = "DELETE FROM Orders WHERE CartId=@cartId and UserId=@userId;";
                string delete_from_carts = "DELETE FROM Carts WHERE CartId=@cartId and UserId=@userId;";
                command.CommandText = delete_from_orders;
                command.ExecuteScalar();
                command.CommandText = delete_from_carts;
                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;

            }
        }
        public List<User> GetAllUsers()
        {
            List<User> users = new List<User>();

            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };

                connection.Open();

                string query = "SELECT * FROM Users WHERE IsDeleted = 0;";
                command.CommandText = query;

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    User user = new User()
                    {
                        Id = Convert.ToInt32(reader["UserId"]),
                        Email = reader["Email"].ToString(),
                        Address = reader["Address"].ToString(),
                        Mobile = reader["Mobile"].ToString(),
                        Password = reader["Password"].ToString(),
                        CreatedAt = reader["CreatedAt"].ToString(),
                        ModifiedAt = reader["ModifiedAt"].ToString(),
                        UserName = reader["UserName"].ToString(),
                        Name = reader["Name"].ToString(),
                        Roles = reader["Role"].ToString(),
                        IsActive = !Convert.ToBoolean(reader["IsDeleted"]) // Assuming IsDeleted represents the user's active status
                    };

                    users.Add(user);
                }

                reader.Close();
            }

            return users;
        }
        public Cart GetActiveCartOfUser(int userid)
        {
            var cart = new Cart();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };
                connection.Open();

                string query = "SELECT COUNT(*) From Carts WHERE UserId=" + userid + " AND Ordered='false';";
                command.CommandText = query;

                int count = (int)command.ExecuteScalar();
                if (count == 0)
                {
                    return cart;
                }

                query = "SELECT CartId From Carts WHERE UserId=" + userid + " AND Ordered='false';";
                command.CommandText = query;

                int cartid = (int)command.ExecuteScalar();

                query = "select * from CartItems where CartId=" + cartid + ";";
                command.CommandText = query;

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    CartItem item = new()
                    {
                        Id = (int)reader["CartItemId"],
                        Product = GetProduct((int)reader["ProductId"])
                    };
                    cart.CartItems.Add(item);
                }

                cart.Id = cartid;
                cart.User = GetUser(userid);
                cart.Ordered = false;
                cart.OrderedOn = "";
            }
            return cart;
        }
        public List<Product> SearchProducts(string query)
        {
            List<Product> products = new List<Product>();

            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };

                connection.Open();

                string searchQuery = @"
            SELECT 
                P.ProductId,
                P.Title,
                P.Description,
                P.Price,
                P.Quantity,
                P.ImageName,
                P.OfferId,
                P.CategoryId,
                PC.CategoryId,
                PC.Category,
                PC.SubCategory,
                O.OfferId,
                O.Title,
                O.Discount
            FROM Products P
            INNER JOIN ProductCategories PC ON P.CategoryId = PC.CategoryId
            LEFT JOIN Offers O ON P.OfferId = O.OfferId
            WHERE P.Title LIKE @query OR P.Description LIKE @query;";

                command.CommandText = searchQuery;
                command.Parameters.AddWithValue("@query", "%" + query + "%");

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ProductCategory productCategory = new ProductCategory()
                    {
                        Category = (string)reader["Category"],
                        SubCategory = (string)reader["SubCategory"]
                    };

                    Offer offer = new Offer()
                    {
                        Id = (int)reader["OfferId"],
                        Title = (string)reader["Title"],
                        Discount = (int)reader["Discount"]
                    };
                    Product product = new Product()
                    {
                        Id = (int)reader["ProductId"],
                        Title = (string)reader["Title"],
                        Description = (string)reader["Description"],
                        ProductCategory = productCategory,
                        Price = (double)reader["Price"],
                        Quantity = (int)reader["Quantity"],
                        ImageName = (string)reader["ImageName"],
                        Offer = offer
                    };

                    products.Add(product);
                }
            }

            return products;
        }

        public List<Product> GetProductsByCategory(string category)
        {
            List<Product> products = new List<Product>();

            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };

                connection.Open();

                string query = "SELECT * FROM Products INNER JOIN ProductCategories ON Products.CategoryId = ProductCategories.CategoryId WHERE ProductCategories.Category = @category;";
                command.CommandText = query;
                command.Parameters.AddWithValue("@category", category);

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Product product = new Product()
                    {
                        Id = (int)reader["ProductId"],
                        Title = (string)reader["Title"],
                        Description = (string)reader["Description"],
                      
                    };

                    products.Add(product);
                }
            }

            return products;
        }


        public List<Cart> GetAllPreviousCartsOfUser(int userid)
        {
            var carts = new List<Cart>();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };
                string query = "SELECT CartId FROM Carts WHERE UserId=" + userid + " AND Ordered='true';";
                command.CommandText = query;
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var cartid = (int)reader["CartId"];
                    carts.Add(GetCart(cartid));
                }
            }
            return carts;
        }

        public Cart GetCart(int cartid)
        {
            var cart = new Cart();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };
                connection.Open();

                string query = "SELECT * FROM CartItems WHERE CartId=" + cartid + ";";
                command.CommandText = query;

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    CartItem item = new()
                    {
                        Id = (int)reader["CartItemId"],
                        Product = GetProduct((int)reader["ProductId"])
                    };
                    cart.CartItems.Add(item);
                }
                reader.Close();

                query = "SELECT * FROM Carts WHERE CartId=" + cartid + ";";
                command.CommandText = query;
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    cart.Id = cartid;
                    cart.User = GetUser((int)reader["UserId"]);
                    cart.Ordered = bool.Parse((string)reader["Ordered"]);
                    cart.OrderedOn = (string)reader["OrderedOn"];
                }
                reader.Close();
            }
            return cart;
        }

        public Offer GetOffer(int id)
        {
            var offer = new Offer();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                string query = "SELECT * FROM Offers WHERE OfferId=" + id + ";";
                command.CommandText = query;

                connection.Open();
                SqlDataReader r = command.ExecuteReader();
                while (r.Read())
                {
                    offer.Id = (int)r["OfferId"];
                    offer.Title = (string)r["Title"];
                    offer.Discount = (int)r["Discount"];
                }
            }
            return offer;
        }

        public List<PaymentMethod> GetPaymentMethods()
        {
            var result = new List<PaymentMethod>();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                string query = "SELECT * FROM PaymentMethods;";
                command.CommandText = query;

                connection.Open();

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    PaymentMethod paymentMethod = new()
                    {
                        Id = (int)reader["PaymentMethodId"],
                        Type = (string)reader["Type"],
                        Provider = (string)reader["Provider"],
                        Available = bool.Parse((string)reader["Available"]),
                        Reason = (string)reader["Reason"]
                    };
                    result.Add(paymentMethod);
                }
            }
            return result;
        }
        public bool DeleteProduct(int id)
        {
            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };

                connection.Open();

                string query = "DELETE FROM Products WHERE ProductId = @id;";
                command.CommandText = query;
                command.Parameters.AddWithValue("@id", id);

                int rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0; // Return true if at least one row was affected (product deleted)
            }
        }
        public bool InsertProduct(Product product)
        {
            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };

                connection.Open();

                string query = "INSERT INTO Products (Title, Description, Price, Quantity, ImageName, CategoryId, OfferId) " +
                               "VALUES (@Title, @Description, @Price, @Quantity, @ImageName, @CategoryId, @OfferId);";

                command.CommandText = query;
                command.Parameters.AddWithValue("@Title", product.Title);
                command.Parameters.AddWithValue("@Description", product.Description);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@Quantity", product.Quantity);
                command.Parameters.AddWithValue("@ImageName", product.ImageName);
                command.Parameters.AddWithValue("@CategoryId", product.ProductCategory.Id);
                command.Parameters.AddWithValue("@OfferId", product.Offer.Id);

                int rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0; // Return true if at least one row was affected (product inserted)
            }
        }
        public bool UpdateProduct(Product product)
        {
            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };

                connection.Open();

                string query = "UPDATE Products SET Title = @Title, Description = @Description, " +
                               "Price = @Price, Quantity = @Quantity, ImageName = @ImageName " +
                               "WHERE ProductId = @Id;";

                command.CommandText = query;
                command.Parameters.AddWithValue("@Title", product.Title);
                command.Parameters.AddWithValue("@Description", product.Description);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@Quantity", product.Quantity);
                command.Parameters.AddWithValue("@ImageName", product.ImageName);
                command.Parameters.AddWithValue("@Id", product.Id);

                int rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0; // Return true if at least one row was affected (product updated)
            }
        }
        public Product GetProduct(int id)
        {
            var product = new Product();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                string query = "SELECT * FROM Products WHERE ProductId=" + id + ";";
                command.CommandText = query;

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    product.Id = (int)reader["ProductId"];
                    product.Title = (string)reader["Title"];
                    product.Description = (string)reader["Description"];
                    product.Price = (double)reader["Price"];
                    product.Quantity = (int)reader["Quantity"];
                    product.ImageName = (string)reader["ImageName"];

                    var categoryid = (int)reader["CategoryId"];
                    product.ProductCategory = GetProductCategory(categoryid);

                    var offerid = (int)reader["OfferId"];
                    product.Offer = GetOffer(offerid);
                }
            }
            return product;
        }

        public List<Product> GetAllProducts()
        {
            List<Product> products = new List<Product>();
            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };

                connection.Open();

                string query = "SELECT * FROM Products;";
                command.CommandText = query;

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Product product = new Product()
                    {
                        Id = (int)reader["ProductId"],
                        Title = (string)reader["Title"],
                        Description = (string)reader["Description"],
                        Price = (double)reader["Price"],
                        Quantity = (int)reader["Quantity"],
                        ImageName = (string)reader["ImageName"]
                    };

                    var categoryId = (int)reader["CategoryId"];
                    product.ProductCategory = GetProductCategory(categoryId);

                    var offerId = (int)reader["OfferId"];
                    product.Offer = GetOffer(offerId);

                    products.Add(product);
                }

                reader.Close();
            }

            return products;
        }

        public List<ProductCategory> GetProductCategories()
        {
            var productCategories = new List<ProductCategory>();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };
                string query = "SELECT * FROM ProductCategories;";
                command.CommandText = query;

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var category = new ProductCategory()
                    {
                        Id = (int)reader["CategoryId"],
                        Category = (string)reader["Category"],
                        SubCategory = (string)reader["SubCategory"]
                    };
                    productCategories.Add(category);
                }
            }
            return productCategories;
        }

        public ProductCategory GetProductCategory(int id)
        {
            var productCategory = new ProductCategory();

            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                string query = "SELECT * FROM ProductCategories WHERE CategoryId=" + id + ";";
                command.CommandText = query;

                connection.Open();
                SqlDataReader r = command.ExecuteReader();
                while (r.Read())
                {
                    productCategory.Id = (int)r["CategoryId"];
                    productCategory.Category = (string)r["Category"];
                    productCategory.SubCategory = (string)r["SubCategory"];
                }
            }

            return productCategory;
        }

        public List<Review> GetProductReviews(int productId)
        {
            var reviews = new List<Review>();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                string query = "SELECT * FROM Reviews WHERE ProductId=" + productId + ";";
                command.CommandText = query;

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var review = new Review()
                    {
                        Id = (int)reader["ReviewId"],
                        Value = (string)reader["Review"],
                        CreatedAt = (string)reader["CreatedAt"]
                    };

                    var userid = (int)reader["UserId"];
                    review.User = GetUser(userid);

                    var productid = (int)reader["ProductId"];
                    review.Product = GetProduct(productid);

                    reviews.Add(review);
                }
            }
            return reviews;
        }

        public List<Product> GetProducts(string category, string subcategory, int count)
        {
            var products = new List<Product>();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                string query = "SELECT TOP " + count + " * FROM Products WHERE CategoryId=(SELECT CategoryId FROM ProductCategories WHERE Category=@c AND SubCategory=@s) ORDER BY newid();";
                command.CommandText = query;
                command.Parameters.Add("@c", System.Data.SqlDbType.NVarChar).Value = category;
                command.Parameters.Add("@s", System.Data.SqlDbType.NVarChar).Value = subcategory;

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var product = new Product()
                    {
                        Id = (int)reader["ProductId"],
                        Title = (string)reader["Title"],
                        Description = (string)reader["Description"],
                        Price = (double)reader["Price"],
                        Quantity = (int)reader["Quantity"],
                        ImageName = (string)reader["ImageName"]
                    };

                    var categoryid = (int)reader["CategoryId"];
                    product.ProductCategory = GetProductCategory(categoryid);

                    var offerid = (int)reader["OfferId"];
                    product.Offer = GetOffer(offerid);

                    products.Add(product);
                }
            }
            return products;
        }
        /* public int AddProduct(Product product)
          {
              using (SqlConnection connection = new SqlConnection(dbconnection))
              {
                  SqlCommand command = new SqlCommand()
                  {
                      Connection = connection
                  };

                  connection.Open();

                  string insertQuery = "INSERT INTO Products (Title, Description, CategoryId, OfferId, Price, Quantity, ImageName) " +
                      "VALUES (@Title, @Description, @CategoryId, @OfferId, @Price, @Quantity, @ImageName); " +
                      "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                  command.CommandText = insertQuery;
                  command.Parameters.AddWithValue("@Title", product.Title);
                  command.Parameters.AddWithValue("@Description", product.Description);
                  command.Parameters.AddWithValue("@CategoryId", product.CategoryId);
                  command.Parameters.AddWithValue("@OfferId", product.OfferId);
                  command.Parameters.AddWithValue("@Price", product.Price);
                  command.Parameters.AddWithValue("@Quantity", product.Quantity);
                  command.Parameters.AddWithValue("@ImageName", product.ImageName);

                  int productId = (int)command.ExecuteScalar();

                  return productId;
              }
          }*/
        public User GetUser(int id)
        {
            var user = new User();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                string query = "SELECT * FROM Users WHERE UserId=" + id + ";";
                command.CommandText = query;

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    user.UserName = (string)reader["UserName"];
                    user.Name = (string)reader["Name"];
                    user.Id = (int)reader["UserId"];
                    user.Email = (string)reader["Email"];
                    user.Address = (string)reader["Address"];
                    user.Mobile = (string)reader["Mobile"];
                    user.Password = (string)reader["Password"];
                    user.CreatedAt = (string)reader["CreatedAt"];
                    user.ModifiedAt = (string)reader["ModifiedAt"];
                    //user.Roles = (string)reader["Role"];
                }
            }
            return user;
        }
        public User GetUserByUserName(string uname)
        {
            var user = new User();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                string query = "SELECT * FROM Users WHERE UserName='" + uname + "';";
                command.CommandText = query;

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    user.UserName = (string)reader["UserName"];
                    user.Name = (string)reader["Name"];
                    user.Id = (int)reader["UserId"];
                    user.Email = (string)reader["Email"];
                    user.Address = (string)reader["Address"];
                    user.Mobile = (string)reader["Mobile"];
                    user.Password = (string)reader["Password"];
                    user.CreatedAt = (string)reader["CreatedAt"];
                    user.ModifiedAt = (string)reader["ModifiedAt"];
                    user.Roles = (string)reader["Role"];
                }
            }
            return user;
        }
        public bool DeleteUser(int id)
        {
            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };

                connection.Open();

                string updateQuery = "UPDATE Users SET IsDeleted = 1 WHERE UserId = @id;";
                command.CommandText = updateQuery;
                command.Parameters.AddWithValue("@id", id);

                int rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0; // Return true if at least one row was affected (user soft deleted)
            }
        }
        public bool InsertCartItem(int userId, int productId)
        {
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                connection.Open();
                string query = "SELECT COUNT(*) FROM Carts WHERE UserId=" + userId + " AND Ordered='false';";
                command.CommandText = query;
                int count = (int)command.ExecuteScalar();
                if (count == 0)
                {
                    query = "INSERT INTO Carts (UserId, Ordered, OrderedOn) VALUES (" + userId + ", 'false', '');";
                    command.CommandText = query;
                    command.ExecuteNonQuery();
                }

                query = "SELECT CartId FROM Carts WHERE UserId=" + userId + " AND Ordered='false';";
                command.CommandText = query;
                int cartId = (int)command.ExecuteScalar();


                query = "INSERT INTO CartItems (CartId, ProductId) VALUES (" + cartId + ", " + productId + ");";
                command.CommandText = query;
                command.ExecuteNonQuery();
                return true;
            }
        }

        public int InsertOrder(Order order)
        {
            int value = 0;

            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                string query = "INSERT INTO Orders (UserId, CartId, PaymentId, CreatedAt) values (@uid, @cid, @pid, @cat);";

                command.CommandText = query;
                command.Parameters.Add("@uid", System.Data.SqlDbType.Int).Value = order.User.Id;
                command.Parameters.Add("@cid", System.Data.SqlDbType.Int).Value = order.Cart.Id;
                command.Parameters.Add("@cat", System.Data.SqlDbType.NVarChar).Value = order.CreatedAt;
                command.Parameters.Add("@pid", System.Data.SqlDbType.Int).Value = order.Payment.Id;

                connection.Open();
                value = command.ExecuteNonQuery();

                if (value > 0)
                {
                    query = "UPDATE Carts SET Ordered='true', OrderedOn='" + DateTime.Now.ToString(dateformat) + "' WHERE CartId=" + order.Cart.Id + ";";
                    command.CommandText = query;
                    command.ExecuteNonQuery();

                    query = "SELECT TOP 1 Id FROM Orders ORDER BY Id DESC;";
                    command.CommandText = query;
                    value = (int)command.ExecuteScalar();
                }
                else
                {
                    value = 0;
                }
            }

            return value;
        }

        public int InsertPayment(Payment payment)
        {
            int value = 0;
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };

                string query = @"INSERT INTO Payments (PaymentMethodId, UserId, TotalAmount, ShippingCharges, AmountReduced, AmountPaid, CreatedAt) 
                                VALUES (@pmid, @uid, @ta, @sc, @ar, @ap, @cat);";

                command.CommandText = query;
                command.Parameters.Add("@pmid", System.Data.SqlDbType.Int).Value = payment.PaymentMethod.Id;
                command.Parameters.Add("@uid", System.Data.SqlDbType.Int).Value = payment.User.Id;
                command.Parameters.Add("@ta", System.Data.SqlDbType.NVarChar).Value = payment.TotalAmount;
                command.Parameters.Add("@sc", System.Data.SqlDbType.NVarChar).Value = payment.ShipingCharges;
                command.Parameters.Add("@ar", System.Data.SqlDbType.NVarChar).Value = payment.AmountReduced;
                command.Parameters.Add("@ap", System.Data.SqlDbType.NVarChar).Value = payment.AmountPaid;
                command.Parameters.Add("@cat", System.Data.SqlDbType.NVarChar).Value = payment.CreatedAt;

                connection.Open();
                value = command.ExecuteNonQuery();

                if (value > 0)
                {
                    query = "SELECT TOP 1 Id FROM Payments ORDER BY Id DESC;";
                    command.CommandText = query;
                    value = (int)command.ExecuteScalar();
                }
                else
                {
                    value = 0;
                }
            }
            return value;
        }

        public void InsertReview(Review review)
        {
            using SqlConnection connection = new(dbconnection);
            SqlCommand command = new()
            {
                Connection = connection
            };

            string query = "INSERT INTO Reviews (UserId, ProductId, Review, CreatedAt) VALUES (@uid, @pid, @rv, @cat);";
            command.CommandText = query;
            command.Parameters.Add("@uid", System.Data.SqlDbType.Int).Value = review.User.Id;
            command.Parameters.Add("@pid", System.Data.SqlDbType.Int).Value = review.Product.Id;
            command.Parameters.Add("@rv", System.Data.SqlDbType.NVarChar).Value = review.Value;
            command.Parameters.Add("@cat", System.Data.SqlDbType.NVarChar).Value = review.CreatedAt;

            connection.Open();
            command.ExecuteNonQuery();
        }

        public bool InsertUser(User user)
        {
            using (SqlConnection connection = new SqlConnection(dbconnection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection
                };
                connection.Open();
                Console.WriteLine("db_in-data");
                string query = "SELECT COUNT(*) FROM Users WHERE Email='" + user.Email + "';";
                Console.WriteLine("db_on-data");
                command.CommandText = query;
                // command.Parameters.AddWithValue("@em", user.Email);
                int count = (int)command.ExecuteScalar();
                if (count > 0)
                {
                    connection.Close();
                    return false;
                }

                query = "INSERT INTO Users (UserName, Name, Address, Mobile, Email, Password, CreatedAt, ModifiedAt, Role) " +
                        "VALUES (@uname, @name, @add, @mb, @em, @pwd, @cat, @mat, @role);";

                command.CommandText = query;
                command.Parameters.AddWithValue("@uname", user.UserName);
                command.Parameters.AddWithValue("@name", user.Name);
                command.Parameters.AddWithValue("@add", user.Address);
                command.Parameters.AddWithValue("@mb", user.Mobile);
                command.Parameters.AddWithValue("@em", user.Email);
                command.Parameters.AddWithValue("@pwd", user.Password);
                command.Parameters.AddWithValue("@cat", user.CreatedAt);
                command.Parameters.AddWithValue("@mat", user.ModifiedAt);
                command.Parameters.AddWithValue("@role", user.Roles);

                command.ExecuteNonQuery();
                Console.WriteLine("db_got-data");
            }
            return true;
        }


        public TokenResponse IsUserPresent(string email, string password)
        {
            User user = new();
            using (SqlConnection connection = new(dbconnection))
            {
                SqlCommand command = new()
                {
                    Connection = connection
                };
                Console.WriteLine(password);
                connection.Open();
                string query = "SELECT COUNT(*) FROM Users WHERE Email='" + email + "' AND Password='" + password + "';";
                command.CommandText = query;
                int count = (int)command.ExecuteScalar();
                if (count == 0)
                {
                    connection.Close();
                    return null;
                }

                query = "SELECT * FROM Users WHERE Email='" + email + "' AND Password='" + password + "';";
                command.CommandText = query;

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    user.Name = (string)reader["Name"];
                    user.UserName = (string)reader["UserName"];
                    user.Id = (int)reader["UserId"];
                    user.Email = (string)reader["Email"];
                    user.Address = (string)reader["Address"];
                    user.Mobile = (string)reader["Mobile"];
                    user.Password = (string)reader["Password"];
                    user.CreatedAt = (string)reader["CreatedAt"];
                    user.ModifiedAt = (string)reader["ModifiedAt"];
                    user.Roles = (string)reader["Role"];
                    //   user.Roles = reader["Role"] != DBNull.Value ? (string)reader["Role"] : string.Empty;
                }

                string key = "MNU66iBl3T5rh6H52i69";
                string duration = "60";
                var symmetrickey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(symmetrickey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim("uname", user.UserName.ToString()),
                    new Claim("name", user.Name.ToString()),
                    new Claim("id", user.Id.ToString()),
                    new Claim("address", user.Address),
                    new Claim("mobile", user.Mobile),
                    new Claim("email", user.Email),
                    new Claim("createdAt", user.CreatedAt),
                    new Claim("modifiedAt", user.ModifiedAt),
                    new Claim("role", user.Roles)

                };

                var token = new JwtSecurityToken(
                    issuer: "localhost",
                    audience: "localhost",
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(Int32.Parse(duration)),
                    signingCredentials: credentials);

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                return new TokenResponse
                {
                    Token = tokenString,
                    Role = user.Roles
                };
            }
        }


    }
}
