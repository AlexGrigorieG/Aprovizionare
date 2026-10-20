using Aprovizionare.Api.Services;

namespace Aprovizionare.Tests;

public class ReceiptAllocatorTests
{
    private const int Motocoasa = 1;
    private const int Lant = 2;
    private const int Manusi = 3;

    [Fact]
    public void Livrare_partiala_se_aloca_integral_pe_linia_de_comanda()
    {
        var openLines = new List<OpenOrderLine> { new(LineId: 10, Motocoasa, OrderedQuantity: 100, OpenQuantity: 100) };

        var allocations = ReceiptAllocator.Allocate(openLines, Motocoasa, 60);

        Assert.Equal(new[] { new Allocation(10, 60) }, allocations);
        Assert.Equal(40m, openLines[0].OpenQuantity);
    }

    [Fact]
    public void Surplusul_ramane_fara_linie_de_comanda()
    {
        var openLines = new List<OpenOrderLine> { new(LineId: 20, Lant, OrderedQuantity: 50, OpenQuantity: 50) };

        var allocations = ReceiptAllocator.Allocate(openLines, Lant, 70);

        Assert.Equal(new[] { new Allocation(20, 50), new Allocation(null, 20) }, allocations);
        Assert.Equal(0m, openLines[0].OpenQuantity);
    }

    [Fact]
    public void Produsul_necomandat_nu_primeste_linie_de_comanda()
    {
        var openLines = new List<OpenOrderLine> { new(LineId: 10, Motocoasa, OrderedQuantity: 100, OpenQuantity: 100) };

        var allocations = ReceiptAllocator.Allocate(openLines, Manusi, 10);

        Assert.Equal(new[] { new Allocation(null, 10) }, allocations);
        Assert.Equal(100m, openLines[0].OpenQuantity);
    }

    [Fact]
    public void A_doua_transa_completeaza_doar_ce_a_ramas_deschis()
    {
        var openLines = new List<OpenOrderLine> { new(LineId: 10, Motocoasa, OrderedQuantity: 100, OpenQuantity: 40) };

        var allocations = ReceiptAllocator.Allocate(openLines, Motocoasa, 45);

        Assert.Equal(new[] { new Allocation(10, 40), new Allocation(null, 5) }, allocations);
    }

    [Fact]
    public void Acelasi_produs_pe_doua_linii_umple_liniile_in_ordine()
    {
        var openLines = new List<OpenOrderLine>
        {
            new(LineId: 10, Motocoasa, OrderedQuantity: 30, OpenQuantity: 30),
            new(LineId: 11, Motocoasa, OrderedQuantity: 20, OpenQuantity: 20)
        };

        var allocations = ReceiptAllocator.Allocate(openLines, Motocoasa, 40);

        Assert.Equal(new[] { new Allocation(10, 30), new Allocation(11, 10) }, allocations);
        Assert.Equal(10m, openLines[1].OpenQuantity);
    }
}
