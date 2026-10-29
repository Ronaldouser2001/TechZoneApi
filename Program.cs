using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configuración de puerto para Render / Docker
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowAll");

app.MapGet("/", () => Results.Ok(new { status = "success", message = "API TechZone activa y lista" }));

#region Helpers para Archivos JSON

string GetFilePath(string fileName)
{
    var baseDir = AppContext.BaseDirectory;
    var path = Path.Combine(baseDir, "Data", fileName);

    if (!File.Exists(path))
    {
        path = Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName);
    }

    if (!File.Exists(path))
    {
        throw new FileNotFoundException($"No se encontró el archivo JSON en: {path}");
    }

    return path;
}

T ReadJson<T>(string fileName)
{
    var path = GetFilePath(fileName);
    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
}

void WriteJson<T>(string fileName, T data)
{
    var path = GetFilePath(fileName);
    var options = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(data, options);
    File.WriteAllText(path, json);
}

#endregion

#region Endpoints CRUD para Productos

// READ (Listar todos / filtrar)
app.MapGet("/api/productos", (int? categoria_id, string? search) =>
{
    try
    {
        var productos = ReadJson<List<Producto>>("productos.json");
        var query = productos.AsEnumerable();
        if (categoria_id.HasValue) query = query.Where(p => p.Categoria_Id == categoria_id.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Nombre.Contains(search, StringComparison.OrdinalIgnoreCase));
        return Results.Ok(new { status = "success", count = query.Count(), data = query.ToList() });
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

// READ (Obtener por ID)
app.MapGet("/api/productos/{id:int}", (int id) =>
{
    try
    {
        var productos = ReadJson<List<Producto>>("productos.json");
        var producto = productos.FirstOrDefault(p => p.Id == id);
        return producto is not null ? Results.Ok(new { status = "success", data = producto }) : Results.NotFound(new { status = "error", message = "Producto no encontrado" });
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

// CREATE (Crear un nuevo producto)
app.MapPost("/api/productos", (ProductoInput input) =>
{
    try
    {
        var productos = ReadJson<List<Producto>>("productos.json");
        int nuevoId = productos.Any() ? productos.Max(p => p.Id) + 1 : 1;

        var nuevoProducto = new Producto(
            nuevoId,
            input.Categoria_Id,
            input.Nombre,
            input.Precio,
            input.Stock,
            input.Descripcion,
            input.Imagen
        );

        productos.Add(nuevoProducto);
        WriteJson("productos.json", productos);

        return Results.Created($"/api/productos/{nuevoId}", new { status = "success", message = "Producto creado con éxito", data = nuevoProducto });
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

// UPDATE (Actualizar un producto existente)
app.MapPut("/api/productos/{id:int}", (int id, ProductoInput input) =>
{
    try
    {
        var productos = ReadJson<List<Producto>>("productos.json");
        int index = productos.FindIndex(p => p.Id == id);

        if (index == -1)
            return Results.NotFound(new { status = "error", message = "Producto no encontrado" });

        var productoActualizado = new Producto(
            id,
            input.Categoria_Id,
            input.Nombre,
            input.Precio,
            input.Stock,
            input.Descripcion,
            input.Imagen
        );

        productos[index] = productoActualizado;
        WriteJson("productos.json", productos);

        return Results.Ok(new { status = "success", message = "Producto actualizado con éxito", data = productoActualizado });
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

// DELETE (Eliminar un producto)
app.MapDelete("/api/productos/{id:int}", (int id) =>
{
    try
    {
        var productos = ReadJson<List<Producto>>("productos.json");
        var producto = productos.FirstOrDefault(p => p.Id == id);

        if (producto is null)
            return Results.NotFound(new { status = "error", message = "Producto no encontrado" });

        productos.Remove(producto);
        WriteJson("productos.json", productos);

        return Results.Ok(new { status = "success", message = "Producto eliminado con éxito" });
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

#endregion

#region Endpoints de Lectura (Categorías, Promociones, Reseñas)

app.MapGet("/api/categorias", () =>
{
    try { return Results.Ok(new { status = "success", data = ReadJson<List<Categoria>>("categorias.json") }); }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

app.MapGet("/api/promociones", () =>
{
    try { return Results.Ok(new { status = "success", data = ReadJson<List<Promocion>>("promociones.json") }); }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

app.MapGet("/api/resenas", (int? producto_id) =>
{
    try
    {
        var resenas = ReadJson<List<Resena>>("resenas.json");
        if (producto_id.HasValue) resenas = resenas.Where(r => r.Producto_Id == producto_id.Value).ToList();
        return Results.Ok(new { status = "success", data = resenas });
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

#endregion

app.Run();

#region DTOs / Records

record Producto(int Id, int Categoria_Id, string Nombre, decimal Precio, int Stock, string Descripcion, string Imagen);
record ProductoInput(int Categoria_Id, string Nombre, decimal Precio, int Stock, string Descripcion, string Imagen);
record Categoria(int Id, string Nombre, string Icono);
record Promocion(int Id, string Titulo, string Descuento, string Codigo);
record Resena(int Id, int Producto_Id, string Usuario, string Comentario, int Estrellas);

#endregion
