﻿namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

// TODO: Removable? 
internal class EditModelValidationException : InvalidOperationException
{
    public EditModelValidationException()
    {
    }

    public EditModelValidationException(string? message) : base(message)
    {
    }

    public EditModelValidationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
