using System.Xml.Linq;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public class XmlReportBuilder
{
    public XDocument BuildReport(IEnumerable<Order> orders)
    {
        var orderList = orders.ToList();

        var byStatus = orderList
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount)
            });

        var byCustomer = orderList
            .GroupBy(o => o.Customer)
            .Select(g => new
            {
                Customer = g.Key,
                Count = g.Count(),
                Total = g.Sum(o => o.TotalAmount),
                Orders = g.ToList()
            });

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("report",
                new XAttribute("generated", DateTime.Now.ToString("o")),

                new XElement("summary",
                    new XAttribute("totalOrders", orderList.Count),
                    new XAttribute("totalRevenue", orderList.Sum(o => o.TotalAmount).ToString("F2"))
                ),

                new XElement("byStatus",
                    byStatus.Select(s =>
                        new XElement("status",
                            new XAttribute("name", s.Status),
                            new XAttribute("count", s.Count),
                            new XAttribute("revenue", s.Revenue.ToString("F2"))
                        )
                    )
                ),

                new XElement("byCustomer",
                    byCustomer.Select(c =>
                        new XElement("customer",
                            new XAttribute("id", c.Customer.Id),
                            new XAttribute("name", c.Customer.Name),
                            new XAttribute("isVip", c.Customer.IsVip.ToString().ToLower()),
                            new XElement("orderCount", c.Count),
                            new XElement("totalSpent", c.Total.ToString("F2")),
                            new XElement("orders",
                                c.Orders.Select(o =>
                                    new XElement("orderRef",
                                        new XAttribute("id", o.Id),
                                        new XAttribute("total", o.TotalAmount.ToString("F2"))
                                    )
                                )
                            )
                        )
                    )
                )
            )
        );
    }

    public async Task SaveReportAsync(XDocument report, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        await Task.Run(() => report.Save(stream));
    }

    public async Task<IEnumerable<int>> FindHighValueOrderIdsAsync(string reportPath, decimal threshold)
    {
        if (!File.Exists(reportPath)) return Enumerable.Empty<int>();

        await using var stream = new FileStream(reportPath, FileMode.Open, FileAccess.Read);
        var doc = await Task.Run(() => XDocument.Load(stream));

        return doc.Descendants("orderRef")
            .Where(el => decimal.Parse(el.Attribute("total")!.Value) > threshold)
            .Select(el => int.Parse(el.Attribute("id")!.Value))
            .Distinct()
            .OrderBy(id => id);
    }
}