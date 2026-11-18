namespace Cart.Application.Exceptions;

public sealed class CartConcurrencyException : Exception
{
    public CartConcurrencyException(Exception innerException)
        : base("The cart was updated by another request. Please retry.", innerException)
    {
    }
}
