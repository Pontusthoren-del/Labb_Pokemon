namespace Labb__Pokémon.Middlewares
{
    public class ErrorMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Console.WriteLine($"I ErrorMiddleware: {context.Request.Path}");
            await _next(context);

            Console.WriteLine($"I ErrorMiddleware: {context.Response.StatusCode}");
            if (context.Response.StatusCode == 404)
            {
                context.Items["Message"] = "Här hittar du inga Pokémons....";
                context.Request.Path = "/Home/Error";

                await _next(context);
            }
        }
    }
}
