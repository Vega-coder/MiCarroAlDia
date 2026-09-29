using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiCarroAlDia.Tests;

public class HttpIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HttpIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Get_Index_ReturnsSuccessAndDemoCards()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Mi Carro al Día", content);
        Assert.Contains("demo-activa", content);
        Assert.Contains("demo-respondida", content);
        Assert.Contains("demo-vencida", content);
        Assert.Contains("demo-sin-adicionales", content);
        Assert.Contains("demo-taller-sur", content);
    }

    [Fact]
    public async Task Get_Tracking_ValidToken_ReturnsSuccessAndVehicleData()
    {
        var response = await _client.GetAsync("/Tracking/demo-activa");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ABC-123", content);
        Assert.Contains("Renault Sandero", content);
        Assert.Contains("Autofrenos del Norte", content);
        Assert.Contains("Juego de pastillas de freno", content);
    }

    [Fact]
    public async Task Get_Tracking_InvalidToken_ReturnsSecurityMessageWithoutDataLeak()
    {
        var response = await _client.GetAsync("/Tracking/token-inexistente-123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Enlace no disponible o inválido", content);
        Assert.DoesNotContain("ABC-123", content);
        Assert.DoesNotContain("XYZ-789", content);
    }

    [Fact]
    public async Task Get_ShortWhatsAppLink_RedirectsToTrackingPage()
    {
        var response = await _client.GetAsync("/t/demo-activa");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Tracking/demo-activa", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_Tracking_ExpiredToken_ShowsPhoneAndVencido()
    {
        var response = await _client.GetAsync("/Tracking/demo-vencida");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("vencido", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("444-1234", content);
    }

    [Fact]
    public async Task Get_Tracking_AnsweredToken_ShowsProofReceipt()
    {
        var response = await _client.GetAsync("/Tracking/demo-respondida");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Comprobante de Respuesta", content);
        Assert.Contains("Total de adicionales autorizados", content);
    }
}
