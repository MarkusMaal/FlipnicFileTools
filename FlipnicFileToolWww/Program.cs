using FlipnicFileToolWww.Components;
using FlipnicLib;
using Syroot.BinaryData;

public class Program
{
    public const string FileString = """
                                     ---------------------------------
                                     Flipnic file tools
                                     ---------------------------------
                                     No file loaded, open a file by clicking "Browse..."
                                     or drag a file to this window.
                                     """;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
    
    

    public static byte[] StreamToBase64(Stream stream)
    {
        
        var data = new byte[stream.Length];
        stream.ReadExactly(data, 0, data.Length);
        return data;
    }

    public static async Task<byte[]> StreamToBytes(Stream st)
    {
        StaticUtils.LiveLoadStatus = "Importing data...";
        var dataL = new List<byte>();
            while (st.CanRead && (st.Position < st.Length - 2048))
            {
                try
                {
                    dataL.AddRange(await Task.Run(() => st.ReadBytesAsync(1024)));
                }
                catch
                {
                    break;
                }
            }

            while (st.CanRead)
            {
                try {
                    dataL.AddRange(await Task.Run(() => st.ReadBytesAsync(1)));
                }
                catch
                {
                    break;
                }
            }

        var data = dataL.ToArray();
        return data;
    }

}