using ECommerce.API.Models;

namespace ECommerce.API.DataAccess
{
    public interface IDataAccess
    {
        List<ProductCategory> GetProductCategories();
        ProductCategory GetProductCategory(int id);
        Offer GetOffer(int id);
        List<Product> GetProducts(string category, string subcategory, int count); 
        Product GetProduct(int id);
        bool InsertUser(User user);
        TokenResponse IsUserPresent(string email, string password);
        void InsertReview(Review review);
        List<Review> GetProductReviews(int productId);
        User GetUser(int id);
        User GetUserByUserName(string uname);
        bool InsertCartItem(int userId, int productId);
        Cart GetActiveCartOfUser(int userid);
        Cart GetCart(int cartid);
        List<Cart> GetAllPreviousCartsOfUser(int userid);
        List<PaymentMethod> GetPaymentMethods();
        int InsertPayment(Payment payment);
        int InsertOrder(Order order);
        bool DeleteCartItem(int userId, int productId);
        bool DeletePreviousCart(int userId, int cartId);

        List<Product> SearchProducts(string query);

        List<Product> GetProductsByCategory(string category);

        List<User> GetAllUsers();
        List<Product> GetAllProducts();
        bool DeleteProduct(int id);
        bool InsertProduct(Product product);
        bool UpdateProduct(Product product);

        bool DeleteUser(int id); // Method for deleting users
    }




}
