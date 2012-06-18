﻿// WPF MapControl - http://wpfmapcontrol.codeplex.com/
// Copyright © 2012 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace MapControl
{
    /// <summary>
    /// Fills a rectangular area with map tiles from a TileSource. If the IsCached property is true,
    /// map tiles are cached in a folder defined by the TileImageLoader.TileCacheFolder property.
    /// </summary>
    [ContentProperty("TileSource")]
    public class TileLayer : DrawingVisual
    {
        private readonly TileImageLoader tileImageLoader = new TileImageLoader();
        private readonly List<Tile> tiles = new List<Tile>();
        private bool isCached = false;
        private string name = string.Empty;
        private string description = string.Empty;
        private Int32Rect grid;
        private int zoomLevel;

        public TileLayer()
        {
            VisualEdgeMode = EdgeMode.Aliased;
            VisualTransform = new MatrixTransform();
            MinZoomLevel = 1;
            MaxZoomLevel = 18;
            MaxDownloads = 8;
        }

        public bool HasDarkBackground { get; set; }
        public int MinZoomLevel { get; set; }
        public int MaxZoomLevel { get; set; }

        public int MaxDownloads
        {
            get { return tileImageLoader.MaxDownloads; }
            set { tileImageLoader.MaxDownloads = value; }
        }

        public TileSource TileSource
        {
            get { return tileImageLoader.TileSource; }
            set { tileImageLoader.TileSource = value; }
        }

        public bool IsCached
        {
            get { return isCached; }
            set
            {
                isCached = value;
                tileImageLoader.TileLayerName = isCached ? name : null;
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                tileImageLoader.TileLayerName = isCached ? name : null;
            }
        }

        public string Description
        {
            get { return description; }
            set { description = value.Replace("{y}", DateTime.Now.Year.ToString()); }
        }

        public Matrix TransformMatrix
        {
            get { return ((MatrixTransform)VisualTransform).Matrix; }
            set { ((MatrixTransform)VisualTransform).Matrix = value; }
        }

        public void UpdateTiles(int zoomLevel, Int32Rect grid)
        {
            this.grid = grid;
            this.zoomLevel = zoomLevel;

            tileImageLoader.StopDownloadTiles();

            if (VisualParent != null && TileSource != null)
            {
                SelectTiles();
                RenderTiles();

                tileImageLoader.StartDownloadTiles(tiles);
            }
        }

        public void ClearTiles()
        {
            tiles.Clear();
            tileImageLoader.StopDownloadTiles();
        }

        private void SelectTiles()
        {
            TileContainer tileContainer = VisualParent as TileContainer;
            int maxZoomLevel = Math.Min(zoomLevel, MaxZoomLevel);
            int minZoomLevel = maxZoomLevel;

            if (tileContainer != null && tileContainer.TileLayers.IndexOf(this) == 0)
            {
                minZoomLevel = MinZoomLevel;
            }

            tiles.RemoveAll(t => t.ZoomLevel < minZoomLevel || t.ZoomLevel > maxZoomLevel);

            for (int z = minZoomLevel; z <= maxZoomLevel; z++)
            {
                int tileSize = 1 << (zoomLevel - z);
                int x1 = grid.X / tileSize;
                int x2 = (grid.X + grid.Width - 1) / tileSize;
                int y1 = Math.Max(0, grid.Y / tileSize);
                int y2 = Math.Min((1 << z) - 1, (grid.Y + grid.Height - 1) / tileSize);

                for (int y = y1; y <= y2; y++)
                {
                    for (int x = x1; x <= x2; x++)
                    {
                        if (tiles.Find(t => t.ZoomLevel == z && t.X == x && t.Y == y) == null)
                        {
                            Tile tile = new Tile(z, x, y);
                            Tile equivalent = tiles.Find(t => t.Image != null && t.ZoomLevel == tile.ZoomLevel && t.XIndex == tile.XIndex && t.Y == tile.Y);

                            if (equivalent != null)
                            {
                                tile.Image = equivalent.Image;
                            }

                            tiles.Add(tile);
                        }
                    }
                }

                tiles.RemoveAll(t => t.ZoomLevel == z && (t.X < x1 || t.X > x2 || t.Y < y1 || t.Y > y2));
            }

            tiles.Sort((t1, t2) => t1.ZoomLevel - t2.ZoomLevel);

            System.Diagnostics.Trace.TraceInformation("{0} Tiles: {1}", tiles.Count, string.Join(", ", tiles.Select(t => t.ZoomLevel.ToString())));
        }

        private void RenderTiles()
        {
            using (DrawingContext drawingContext = RenderOpen())
            {
                tiles.ForEach(tile =>
                {
                    int tileSize = 256 << (zoomLevel - tile.ZoomLevel);
                    Rect tileRect = new Rect(tileSize * tile.X - 256 * grid.X, tileSize * tile.Y - 256 * grid.Y, tileSize, tileSize);

                    drawingContext.DrawRectangle(tile.Brush, null, tileRect);

                    //if (tile.ZoomLevel == zoomLevel)
                    //    drawingContext.DrawText(new FormattedText(tile.ToString(), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black), tileRect.TopLeft);
                });
            }
        }
    }
}