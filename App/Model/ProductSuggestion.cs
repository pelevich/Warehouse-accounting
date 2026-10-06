namespace App.Model
{
    //Model для поиска по совпадениям
    public class ProductSuggestion
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Quantity { get; set; }
    }
}
