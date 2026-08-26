using System;
using Terminal.Gui.ViewBase;

namespace ZtrBoardGame.Server.Desktop;

public sealed class WrapView : View
{
    public int HorizontalSpacing { get; set; } = 1;
    public int VerticalSpacing { get; set; } = 0;

    public WrapView()
    {
        // Kontrolka ma wypełnić dostępną szerokość.
        // Wysokość dopasuje się automatycznie do ułożonych wewnątrz dzieci.
        Width = Dim.Fill();
        Height = Dim.Auto();

        // Podpinamy się pod moment, w którym silnik v2 przelicza układ
        DrawComplete += (_, _) => UpdateWrapLayout();
    }

    private void UpdateWrapLayout()
    {
        var currentX = 0;
        var currentY = 0;
        var maxRowHeight = 0;

        // WAŻNE w v2: 'Bounds' zostało zastąpione przez 'Viewport'
        var maxWidth = Viewport.Width;

        foreach (var subview in SubViews)
        {
            if (!subview.Visible)
            {
                continue;
            }

            // W v2 rozmiar wyliczany kontrolki jest trzymany w 'Frame'
            var width = subview.Frame.Width;
            var height = subview.Frame.Height;

            // Jeśli element nie mieści się w rzędzie (i to nie jest pierwszy element rzędu) -> przenosimy do nowej linii
            if (currentX + width > maxWidth && currentX > 0)
            {
                currentX = 0;
                currentY += maxRowHeight + VerticalSpacing;
                maxRowHeight = 0;
            }

            // Nadpisujemy pozycję absolutną, omijając domyślny system relatywny (Pos)
            subview.X = currentX;
            subview.Y = currentY;

            // Przesuwamy wirtualny kursor o szerokość elementu i odstęp
            currentX += width + HorizontalSpacing;
            maxRowHeight = Math.Max(maxRowHeight, height);
        }
    }
}
