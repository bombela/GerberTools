﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GerberLibrary;
using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;

namespace PnP_Processor
{
    public class PnPProcDoc : ProgressLog
    {

        StandardConsoleLog log;
        public string stock;
        public string silk;
        public string pnp;
        public string bom;
        public string outline;
        public string gerberzip;
        public bool loaded = false;
        public PointD FixOffset = new PointD();
        public bool FlipBoard = false;
        public int RotationAngle = 0;

        public PnPProcDoc()
        {
            log = new StandardConsoleLog(this);
        }

        Thread T = null;

        public void StartLoad()
        {
            T = new Thread(new ThreadStart(LoadStuff));
            T.Start();
        }
        private static GerberImageCreator LoadGerberZip(string v, ProgressLog log)
        {
            log.PushActivity("LoadingGerbers");
            GerberImageCreator GIC = new GerberImageCreator();
            List<String> Files = new List<string>();
            Files.Add(v);
            GIC.AddBoardsToSet(Files, log,true, false);

            // log.AddString(GIC.GetOutlineBoundingBox().ToString());
            log.PopActivity();
            return GIC;
        }
        public BOM B = new BOM();
        public BOM BPost = new BOM();

        public void BuildPostBom()
        {
            BPost = new BOM();
            BOMNumberSet s = new BOMNumberSet();
            if (FlipBoard)
            {
                FixOffset = new PointD(Set.BoundingBox.BottomRight.X, Set.BoundingBox.TopLeft.Y);
            }
            else
            {
                FixOffset = new PointD(Set.BoundingBox.TopLeft.X, Set.BoundingBox.TopLeft.Y);
            }

            BPost.MergeBOM(B, s, 0, 0, -FixOffset.X, -FixOffset.Y, 0);
            
            FixSet = new GerberImageCreator();
            FixSet.CopyFrom(Set);
            

            if (FlipBoard)
            {
                FixSet.SetBottomRightToZero();
                FixSet.FlipXY();
                FixSet.Translate(0, FixSet.BoundingBox.Height());
                BPost.SwapXY();
                BPost.FlipSides();
                BPost.Translate(0, FixSet.BoundingBox.Height());
            }
            else
            {
                FixSet.SetBottomLeftToZero();
            }
            BPost.FixupAngles(StockDoc);
        }

        public GerberImageCreator Set;
        public GerberImageCreator FixSet;
        private void LoadStuff()
        {

            if (stock.Length > 0 && File.Exists(stock))
            {
                try
                {
                    log.PushActivity("Loading stock");
                    StockDoc = StockDocument.Load(stock);
                    if (StockDoc == null) StockDoc = new StockDocument();
                }
                catch(Exception)
                {
                    StockDoc = new StockDocument();
                }
                 log.PopActivity();
            }
            else
            {
                StockDoc = new StockDocument();
            }
            log.PushActivity("Loading document");
            B = new BOM();

            if (bom.Length > 0 && pnp.Length > 0)
            {
                String DirBaseName = Path.GetFileNameWithoutExtension(pnp);
                log.PushActivity("Processing " + DirBaseName);

                log.PushActivity("Loading BOM");
                log.AddString(String.Format("Loading BOM! {0},{1}", Path.GetFileName(bom), Path.GetFileName(pnp)));
                B.LoadJLC(bom, pnp);
                log.PopActivity();

                if (gerberzip != null && File.Exists(gerberzip))
                {
                    Set = LoadGerberZip(gerberzip, log);
                }
                else
                {
                    Set = new GerberImageCreator();
                    Set.AddBoardToSet(silk, log);
                    Set.AddBoardToSet(outline, log);
                }
                Box = Set.BoundingBox;

                BuildPostBom();

                log.PopActivity();
            }
            else
            {
                log.AddString(String.Format("pnp and bom need to be valid! bom:{0} pnp:{1}", bom, pnp));
            }

            
            loaded = true;
            log.AddString("Done!");
            log.PopActivity();
        }

        public List<string> Log = new List<string>();
        public int Stamp = 0;
        static int MainStamp = 0;
        public Bounds Box = new Bounds();
        private StockDocument StockDoc;

        public override void AddString(string text, float progress = -1)
        {
            Stamp = ++MainStamp;
            Log.Add(text);
        }
    }
}
