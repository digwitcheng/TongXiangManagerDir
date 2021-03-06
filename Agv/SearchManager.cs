﻿using AGV_V1._0.Agv;
using AGV_V1._0.Util;
using Agv.PathPlanning;
using System.Collections.Generic;
using System.Windows.Forms;
using AGV_V1._0.Algorithm;

namespace AGV_V1._0
{
    class SearchManager
    {
        ElecMap Elc;
        AgvPathPlanning astarSearch;
        //private readonly object searchLock = new object();

        private static SearchManager _instance;
        public static SearchManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SearchManager();
                }
                return _instance;
            }
        }
        public SearchManager()
        {
            Elc = ElecMap.Instance;
            astarSearch = new AgvPathPlanning();

        }
        private int ResearchCount
        {
            get;
            set;

        }
        public void ReSearchRoute(Vehicle v)
        {
            if (!v.EqualWithRealLocation(v.Route[v.TPtr].X, v.Route[v.TPtr].Y))
            {
                return;
            }
            v.StopTime = ConstDefine.STOP_TIME;
            ResearchCount++;
            if (v.Route == null || v.Route.Count <= v.VirtualTPtr)
            {
                return;
            }
            v.LockNode.Add(v.Route[v.VirtualTPtr]);
            for (int i = 0; i < v.TPtr; i++)
            {
                v.SetNodeCanUsed(v.Route[i].X, v.Route[i].Y);
            }
            v.BeginX = v.Route[v.TPtr].X;
            v.BeginY = v.Route[v.TPtr].Y;
            //Task.Factory.StartNew(() => SearchRoute(Elc), TaskCreationOptions.None);
            SearchRoute(v);
            //}
            // Elc.mapnode[Route[Virtual_tPtr].X, Route[Virtual_tPtr].Y].LockNode = -1;
        }
        private static object searchLock = new object();
        public void SearchRoute(Vehicle v)
        {
            lock (searchLock)
            {
                if (v.agvInfo == null)
                {
                    return;
                }
                v.BeginX = v.GetRealX();
                v.BeginY = v.GetRealY();
                v.RouteIndex = 0;
                v.cost = 0;
                v.TPtr = 0;// tFram = 0;
                v.Finished = false;
                v.Arrive = false;
                v.StopTime = ConstDefine.STOP_TIME;
                if (!checkXY(v))
                {
                    v.CurState = State.cannotToDestination;
                    MessageBox.Show("起点或终点超出地图界限");
                    return;
                }
                ////AstarSearch astarSearch = new AstarSearch(Elc);
                List<MyPoint> scannerNode = new List<MyPoint>();
                bool isSpecialArea = false;
                if (!Elc.IsSpecialArea(v.BeginX, v.BeginY))
                {
                    scannerNode = Elc.GetScanner();
                }
                else
                {
                    if (Elc.IsSpecialArea(v.EndX, v.EndY))//开始起点都是特殊区（从扫描仪绕到排队入口）
                    {
                        MyPoint nextEnd = ElecMap.Instance.CalculateScannerPoint(new MyPoint(v.EndX, v.EndY, Direction.Right));
                        v.EndX = nextEnd.X;
                        v.EndY = nextEnd.Y;
                        v.StartLoc = "DestArea";// "WaitArea";
                        v.EndLoc = "ScanArea";// "ScanArea";
                        isSpecialArea = true;
                    }
                }
                List<MyPoint> routeList = astarSearch.Search(Elc, scannerNode, v.LockNode, v.BeginX, v.BeginY, v.EndX, v.EndY, v.Dir, v.algorithm);
                // Elc.mapnode[startX, startY].NodeCanUsed = false;//搜索完,小车自己所在的地方被小车占用           
                if (routeList.Count < 1)
                {
                    // MessageBox.Show("没有搜索到路线:"+v_num);
                    v.CurState = State.cannotToDestination;
                    //v.LockNode.cl;
                }
                else
                {
                    if (isSpecialArea)
                    {
                        v.Route = routeList;
                        return;
                    }
                    if (Elc.IsQueueEntra(v.EndX, v.EndY))
                    {
                        MyPoint nextEnd = ElecMap.Instance.CalculateScannerPoint(new MyPoint(v.EndX, v.EndY, Direction.Right));
                        if (nextEnd.X == v.BeginX && nextEnd.Y == v.BeginY)
                        {
                            v.Route = new List<MyPoint>();
                            v.Arrive = true;
                            v.CurState = State.Free;
                            return;
                        }
                        List<MyPoint> addRoute = astarSearch.Search(Elc, new List<MyPoint>(), v.LockNode, v.EndX, v.EndY, nextEnd.X, nextEnd.Y, v.Dir, v.algorithm);
                        if (addRoute != null && addRoute.Count > 1)
                        {
                            for (int i = 1; i < addRoute.Count; i++)
                            {
                                routeList.Add(addRoute[i]);
                            }
                            v.EndX = nextEnd.X;
                            v.EndY = nextEnd.Y;
                            v.EndLoc = "ScanArea";
                        }
                    }
                    v.Route = routeList;
                }
            }
        }

        bool checkXY(Vehicle v)
        {
            if (v == null)
            {
                return false;
            }
            if (Elc.IsLegalLocation(v.BeginX, v.BeginY) && Elc.IsLegalLocation(v.EndX, v.EndY))
            {
                return true;
            }
            return false;
        }


    }
}
