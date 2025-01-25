using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var pdfBytes = await BuildInvoicePdf();
        File.WriteAllBytes("Invoice.pdf", pdfBytes);

        Console.WriteLine("PDF generated successfully: Invoice.pdf");
        SaveByteArrayToPdf(pdfBytes, "Invoice.pdf");

    }
    private static async Task<byte[]> BuildInvoicePdf()
    {
        var task = Task.Run(() =>
        {
            using var memoryStream = new MemoryStream();

            using var pdfDocument = new PdfDocument();
            pdfDocument.PageSettings.Size = PdfPageSize.A2;
            PdfPage currentPage = pdfDocument.Pages.Add();
            pdfDocument.PageSettings.Margins.Top = 800;
            pdfDocument.FileStructure.IncrementalUpdate = false;
            pdfDocument.FileStructure.EnableTrailerId = true;

            SizeF clientSize = currentPage.GetClientSize();

            PdfGraphics graphics = currentPage.Graphics;
            PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
            PdfFont Font = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Regular);
            PdfStringFormat stringFormat = new PdfStringFormat();
            stringFormat.LineSpacing = 5;

            float leftMargin = 14;
            float rightMargin = clientSize.Width - 25;

            var fromHeader = new PdfTextElement("From", headerFont);
            var fromHeaderResult = fromHeader.Draw(currentPage, new PointF(leftMargin, 13 + 60));

            var fromAddress = new PdfTextElement("Hallmark Healthcare Solutions, Inc,\n200 Motor Parkway, Suite D#26\nHauppauge, NY 11788", Font) { StringFormat = stringFormat };
            var fromAddressResult = fromAddress.Draw(currentPage, new PointF(leftMargin + 20, 13 + 100));

            fromHeader = new PdfTextElement("To", headerFont);
            fromHeaderResult = fromHeader.Draw(currentPage, new PointF(leftMargin, 13 + 175));

            fromAddress = new PdfTextElement("Community Regional Medical Center\n2823 Fresno St,\nFresno, California 93721", Font) { StringFormat = stringFormat };
            fromAddressResult = fromAddress.Draw(currentPage, new PointF(leftMargin + 20, 13 + 210));

            var invoiceText = new PdfTextElement("Invoice No: AZH-195\nInvoice Date: 10/21/2024\nNet Payment Terms: 45\nDue Date: 12/05/2024", Font);
            invoiceText.StringFormat = new PdfStringFormat(PdfTextAlignment.Right);
            PdfLayoutResult invoiceResult = invoiceText.Draw(currentPage, new PointF(rightMargin, 13 + 40));

            var data = GetInvoiceData();
            var result = data.FirstOrDefault();
            var count = result.GetType().GetProperties().Select(x => x.GetValue(result) != null).Count(x => x == true);

            var pdfGrid = new PdfGrid();
            pdfGrid.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Regular); ;
            pdfGrid.Columns.Add(count);
            pdfGrid.Headers.Add(2);
            PdfGridRow fakePdfGridheader = pdfGrid.Headers[0];
            fakePdfGridheader.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 6.8f, PdfFontStyle.Bold);
            pdfGrid.Style.BackgroundBrush = PdfBrushes.White;
            PdfPen darkBorderPen = new PdfPen(new PdfColor(0, 0, 0), 1.5f);
            pdfGrid.Style.CellPadding = new PdfPaddings(4, 2, 8, 0);
            pdfGrid.Style.HorizontalOverflowType = PdfHorizontalOverflowType.NextPage;


            Type type = result.GetType();
            PropertyInfo[] properties = type.GetProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                var displayNameAttribute = properties[i].GetCustomAttribute<DisplayNameAttribute>();
                string headerDisplayName = displayNameAttribute?.DisplayName ?? properties[i].Name;

                fakePdfGridheader.Cells[i].Value = headerDisplayName;
                fakePdfGridheader.Cells[i].StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Top);
                fakePdfGridheader.Cells[i].Style.TextBrush = PdfBrushes.White;
                fakePdfGridheader.Cells[i].Style.Borders.All = new PdfPen(new PdfColor(255, 255, 255), 1.5f);
            }

            PdfGridRow pdfGridHeader = pdfGrid.Headers[1];
            pdfGridHeader.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
            pdfGridHeader.Style.BackgroundBrush = PdfBrushes.LightGray;
            pdfGrid.Style.CellPadding = new PdfPaddings(4, 2, 8, 0);
            pdfGrid.Style.HorizontalOverflowType = PdfHorizontalOverflowType.NextPage;

            for (int i = 0; i < properties.Length; i++)
            {
                var displayNameAttribute = properties[i].GetCustomAttribute<DisplayNameAttribute>();
                string headerDisplayName = displayNameAttribute?.DisplayName ?? properties[i].Name;

                pdfGridHeader.Cells[i].Value = headerDisplayName;
                pdfGridHeader.Cells[i].StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);
                pdfGridHeader.Cells[i].Style.Borders.All = darkBorderPen;
            }

            decimal totalvalue = 0;
            foreach (var rowData in data)
            {
                PdfGridRow row = pdfGrid.Rows.Add();
                row.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Regular);
                row.Style.BackgroundBrush = PdfBrushes.White;
                Type types = rowData.GetType();
                PropertyInfo[] property = type.GetProperties();
                PdfStringFormat fort = new PdfStringFormat();
                fort.WordWrap = PdfWordWrapType.Word;

                Console.WriteLine($"{pdfDocument.Pages.Count} inside for");

                for (int i = 0; i < property.Length; i++)
                {
                    var value = property[i].GetValue(rowData);
                    if (property[i].Name == "Total" || property[i].Name == "BillRate")
                    {
                        totalvalue += property[i].Name == "Total" ? value as decimal? ?? Convert.ToDecimal(value ?? 0) : 0;
                        row.Cells[i].Value = $"${value?.ToString()}" ?? string.Empty;
                        row.Cells[i].StringFormat = fort;

                    }
                    else
                    {
                        row.Cells[i].Value = value?.ToString() ?? string.Empty;
                        row.Cells[i].StringFormat = fort;

                    }
                    row.Cells[i].Style.Borders.All = darkBorderPen;

                }
            }

            PdfLayoutFormat layoutFormat = new PdfLayoutFormat
            {
                Break = PdfLayoutBreakType.FitPage,
                Layout = PdfLayoutType.Paginate
            };

            PdfGridRow footerRow = pdfGrid.Rows.Add();
            int totalColumns = pdfGrid.Columns.Count;
            footerRow.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Regular);
            footerRow.Style.BackgroundBrush = PdfBrushes.White;
            footerRow.Cells[0].ColumnSpan = totalColumns - 1;
            footerRow.Cells[0].Value = "Invoice Amount:";
            footerRow.Cells[0].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
            footerRow.Cells[0].StringFormat = new PdfStringFormat(PdfTextAlignment.Right);
            footerRow.Cells[0].Style.CellPadding = new PdfPaddings(0, 5, 8, 5);
            footerRow.Cells[0].Style.Borders.All = darkBorderPen;
            footerRow.Cells[totalColumns - 1].Value = $"${totalvalue}";
            footerRow.Cells[totalColumns - 1].Style.Borders.All = darkBorderPen;

            pdfGrid.AllowRowBreakAcrossPages = true;
            pdfGrid.RepeatHeader = true;
            var pdfGridLayoutResult = pdfGrid.Draw(currentPage, new PointF(0, 310), layoutFormat);
            pdfGrid.RepeatHeader = false;

            var summarydata = GetSummaryData();
            var summaryresult = summarydata.FirstOrDefault();
            PdfGrid summaryHeaderGrid = new PdfGrid();
            summaryHeaderGrid.Columns.Add(6);
            summaryHeaderGrid.Headers.Add(2);

            PdfGridRow summaryGridheader = summaryHeaderGrid.Headers[0];
            summaryGridheader.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
            summaryGridheader.Style.BackgroundBrush = PdfBrushes.LightGray;
            summaryHeaderGrid.Style.CellPadding = new PdfPaddings(4, 2, 8, 0);
            summaryHeaderGrid.Style.HorizontalOverflowType = PdfHorizontalOverflowType.NextPage;
            summaryGridheader.Cells[0].Value = "Summary";
            summaryGridheader.Cells[0].Style.Font = headerFont;
            summaryGridheader.Cells[0].Style.BackgroundBrush = PdfBrushes.LightGray;
            summaryGridheader.Cells[0].Style.StringFormat = new PdfStringFormat(PdfTextAlignment.Center);
            summaryGridheader.Cells[0].ColumnSpan = 6;

            PdfGridRow summarygridheader2 = summaryHeaderGrid.Headers[1];
            summarygridheader2.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
            summarygridheader2.Style.BackgroundBrush = PdfBrushes.LightGray;
            summaryHeaderGrid.Style.CellPadding = new PdfPaddings(4, 2, 8, 0);

            List<string> summaryheaderinfo = new List<string>() { "Location", "Department", "Skill", "Hours/Miles", "Total Amount", "Addl.Details" };
            for (int i = 0; i < summaryheaderinfo.Count; i++)
            {
                summarygridheader2.Cells[i].Value = summaryheaderinfo[i];
                summarygridheader2.Cells[i].StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);
                summarygridheader2.Cells[i].Style.Borders.All = darkBorderPen;
            }
            summaryHeaderGrid.Draw(pdfDocument.Pages[pdfDocument.Pages.Count - 1], new PointF(0, pdfGridLayoutResult.Bounds.Bottom + 40));

            //List<string> summaryrowinfo = new List<string>() { "DepartmentName", "SkillName", "Value", "TotalAmount", "Addl.Details" };


            //foreach (var summaryrowData in summarydata)
            //{
            //    PdfGridRow summaryrow = summaryHeaderGrid.Rows.Add();
            //    summaryrow.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Regular);
            //    summaryrow.Style.BackgroundBrush = PdfBrushes.White;
            //    PdfStringFormat format = new PdfStringFormat();
            //    format.WordWrap = PdfWordWrapType.Word;
            //    summaryrow.Cells[0].Value = summaryrowData.LocationName;
            //    summaryrow.Cells[0].StringFormat = format;
            //    summaryrow.Cells[0].Style.Borders.All = darkBorderPen;
            //    Dictionary<string, string> propertyValues = new Dictionary<string, string>();

            //    foreach (var sumdata in summaryrowData.Items)
            //    {
            //        Type types = sumdata.GetType();
            //        PropertyInfo[] summaryProperties = types.GetProperties();

            //        foreach (var property in summaryProperties)
            //        {
            //            object value = property.GetValue(sumdata);
            //            propertyValues[property.Name] = value?.ToString() ?? string.Empty;
            //        }
            //        for (int i = 1; i <= summaryrowinfo.Count; i++)
            //        {
            //            string columnKey = summaryrowinfo[i - 1];// Adjust index since summaryrowinfo is 0-based

            //            if (propertyValues.TryGetValue(columnKey, out string columnValue))
            //            {
            //                summaryrow.Cells[i].Value = columnValue;
            //            }
            //            else if (columnKey == "Addl.Details")
            //            {
            //                summaryrow.Cells[i].Value = $"{propertyValues["LocationIExternalId"]}-{propertyValues["DepartmentName"]}-{propertyValues["SkillGLNumber"]}";
            //            }

            //            summaryrow.Cells[i].StringFormat = format;
            //            summaryrow.Cells[i].Style.Borders.All = darkBorderPen;
            //            if (summaryrowinfo.Count - 1 == i)
            //            {
            //                summaryrow = summaryHeaderGrid.Rows.Add();
            //            }
            //        }


            //    }

            //}

            //summaryHeaderGrid.Draw(pdfDocument.Pages[pdfDocument.Pages.Count - 1], new PointF(0, pdfGridLayoutResult.Bounds.Bottom + 40));
            //Console.WriteLine($"{pdfDocument.Pages.Count} after draw");


            //foreach (PdfPage page in pdfDocument.Pages)
            //{
            //    PdfGraphics pageGraphics = page.Graphics;

            //    RectangleF bounds = new RectangleF(0, 0, page.GetClientSize().Width, 50);
            //    PdfPageTemplateElement header = new PdfPageTemplateElement(bounds);

            //    var stream = Logo();
            //    PdfImage image = new PdfBitmap(stream);

            //    SizeF iconSize = new SizeF(120, 33);
            //    PointF iconLocation = new PointF(14, 13);
            //    pdfDocument.Template.Top = header;
            //    pageGraphics.DrawImage(image, new PointF(0, 0), iconSize);
            //    stream.Dispose();

            //    PdfPageTemplateElement footer = new PdfPageTemplateElement(bounds);
            //    PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
            //    PdfBrush brush = new PdfSolidBrush(Color.Black);
            //    PdfPageNumberField pageNumber = new PdfPageNumberField(font, brush);
            //    PdfPageCountField counts = new PdfPageCountField(font, brush);
            //    PdfCompositeField pageText = new PdfCompositeField(font, brush, "Page {0} of {1}", pageNumber, counts);
            //    pageText.Bounds = footer.Bounds;
            //    PdfCompositeField date = new PdfCompositeField(font, brush, $"Date : {DateTime.UtcNow.Date.ToString("dd/MM/yyyy")}");
            //    date.Bounds = footer.Bounds;
            //    PdfCompositeField copyRights = new PdfCompositeField(font, brush, $"©{DateTime.UtcNow.Year} Copyright Reserved by Einstein ll");
            //    copyRights.Bounds = footer.Bounds;
            //    pageText.Draw(footer.Graphics, new PointF(1035, 37));
            //    date.Draw(footer.Graphics, new PointF(15, 37));
            //    copyRights.Draw(footer.Graphics, new PointF(500, 37));
            //    pdfDocument.Template.Bottom = footer;
            //}
            //if (pdfDocument.Pages.Count == 0)
            //{
            //    throw new InvalidOperationException("No pages available in the PDF document.");
            //}
            pdfDocument.Save(memoryStream);

            return memoryStream.ToArray();
        });

        return await task;
    }
    private static List<ExportInvoiceDto> GetInvoiceData()
    {
        return new List<ExportInvoiceDto>
        {
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH) ",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/03/2024 06:15",
                TimeOut = "12/03/2024 07:45",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)125.50,
                Total = (decimal)188.25
            },
            new ExportInvoiceDto
            {
                InvoiceId  = "PHC-298",
                LocationName = "Defiance Hospital",
                Week = "49-Sun, 12/01/2024",
                TimeIn = "12/07/2024 00:00",
                TimeOut = "12/07/2024 00:00",
                CostCenter = "FAMILY DOCTORS",
                JobId = "THS-206-1",
                CandidateName = "Test Candidate",
                Agency = "Regions Staffing Agency A",
                Skill = "Anesthesia Technician",
                HoursMiles = (decimal?)1.50,
                BillRateTypeCodeReason = "Guaranteed Hours",
                BillRate = (decimal?)125.50,
                Total = (decimal)4831.75
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/29/2024 08:00",
                TimeOut = "12/29/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.00,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1140.00
            },
            new ExportInvoiceDto
            {
                InvoiceId = "PHC-298",
                LocationName = "Brigham & Women's Hospital(BWH)",
                Week = "53-Sun, 12/29/2024",
                TimeIn = "12/31/2024 08:00",
                TimeOut = "12/31/2024 20:30",
                CostCenter = "PS-EMERGENCY DEPT Staff  (BW5511)",
                JobId = "MGB-15710-9",
                CandidateName = "Natalie Smith",
                Agency = "SHC Services Inc dba Supplemental Health Care",
                Skill = "RN Emergency Room",
                HoursMiles = (decimal?)12.50,
                BillRateTypeCodeReason = "Regular",
                BillRate = (decimal?)95.00,
                Total = (decimal)1187.50
            }
        };
    }
    private static List<PrintGroupedSummaryDto> GetSummaryData()
    {
        return new List<PrintGroupedSummaryDto>
    {
        new PrintGroupedSummaryDto
        {
            LocationName = "Cooley Dickinson VNA & Hospice (CDV)",
            Items = new List<PrintInvoiceSummaryRecordDto>
            {
                new PrintInvoiceSummaryRecordDto
                {
                    DepartmentName = "PS-VNANurseWeekend",
                    ExtDepartmentId = "CDV032",
                    LocationIExternalId = "1930",
                    LocationInvoiceId = "1930",
                    InvoiceDepartmentId = "CV9415",
                    SkillGLNumber = "823901",
                    SkillName = "RN Home Health",
                    Value = 12,
                    Total = 1260,
                    CalculatedTotal = 1260,
                    Fee = 0,
                    FeeTotal = 0,
                    Details = null,
                    SalesTaxFee = 0,
                    SalesTaxAmount = 0,
                    TotalAmount = 1260
                },
                new PrintInvoiceSummaryRecordDto
                {
                    DepartmentName = "PS-VNANurseWeekend",
                    ExtDepartmentId = "CDV032",
                    LocationIExternalId = "1930",
                    LocationInvoiceId = "1930",
                    InvoiceDepartmentId = "CV9415",
                    SkillGLNumber = "823901",
                    SkillName = "RN Home Health",
                    Value = 12,
                    Total = 1260,
                    CalculatedTotal = 1260,
                    Fee = 0,
                    FeeTotal = 0,
                    Details = null,
                    SalesTaxFee = 0,
                    SalesTaxAmount = 0,
                    TotalAmount = 1260
                }
            }
        }
    };
    }
    static void SaveByteArrayToPdf(byte[] byteArray, string filePath)
    {
        try
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fileStream.Write(byteArray, 0, byteArray.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving PDF: {ex.Message}");
        }
    }
    static FileStream Logo()
    {
        string base64String = "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";
        byte[] imageBytes = Convert.FromBase64String(base64String);
        FileStream fileStream = new FileStream("output1.PNG ", FileMode.Create);
        fileStream.Write(imageBytes, 0, imageBytes.Length);
        return fileStream;

    }
    public class ExportInvoiceDto
    {
        [DisplayName("Invoice ID")]
        public string InvoiceId { get; set; }

        [DisplayName("Location Name")]
        public string LocationName { get; set; }

        public string Week { get; set; }
        [DisplayName("Time In")]
        public string? TimeIn { get; set; }
        [DisplayName("Time Out")]
        public string? TimeOut { get; set; }

        [DisplayName("Cost Center")]
        public string CostCenter { get; set; }

        [DisplayName("Job ID")]
        public string JobId { get; set; }
        [DisplayName("Candidate Name")]
        public string CandidateName { get; set; }
        public string Agency { get; set; }
        public string Skill { get; set; }
        [DisplayName("Hours / Miles")]
        public decimal? HoursMiles { get; set; }
        [DisplayName("Bill Rate Type / Expenses Code Reason")]
        public string BillRateTypeCodeReason { get; set; }

        [DisplayName("Bill Rate")]
        public decimal? BillRate { get; set; }
        public decimal Total { get; set; }
    }
    public class PrintGroupedSummaryDto
    {
        public string LocationName { get; set; }
        public List<PrintInvoiceSummaryRecordDto> Items { get; set; }
    }
    public class PrintInvoiceSummaryRecordDto
    {
        public string DepartmentName { get; set; }
        public string ExtDepartmentId { get; set; }
        public string? LocationIExternalId { get; set; }
        public string? LocationInvoiceId { get; set; }
        public string? InvoiceDepartmentId { get; set; }
        public string? SkillGLNumber { get; set; }
        public string CostCenterFormattedName => string.IsNullOrEmpty(ExtDepartmentId) ? $"{DepartmentName}" : $"{DepartmentName}-{ExtDepartmentId}";
        public string SkillName { get; set; }
        public decimal Value { get; set; }
        public decimal Total { get; set; }
        public decimal CalculatedTotal { get; set; }
        public decimal Fee { get; set; }
        public decimal FeeTotal { get; set; }
        public string Details { get; set; }
        public decimal SalesTaxFee { get; set; }
        public decimal SalesTaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}