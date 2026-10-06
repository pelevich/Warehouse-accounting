using Services.DTOs;

namespace App.Messages
{
    public sealed record ProductAddedMessage(ProductDto product);
}
