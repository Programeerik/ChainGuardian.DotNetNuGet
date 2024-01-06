using Refit;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var test = new RefitSettings().HttpMessageHandlerFactory?.Invoke() ?? new HttpClientHandler();