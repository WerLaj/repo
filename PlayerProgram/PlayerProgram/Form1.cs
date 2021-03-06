﻿using MessageProject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Net.Sockets;
using System.Net;
using System.Threading;
namespace PlayerProgram
{
    public partial class Form1 : Form
    {
        static Dictionary<Type, int> typeDict = new Dictionary<Type, int>
        {
            {typeof(ConfirmGameRegistration),0},
            {typeof(ConfirmJoiningGame),1},
            {typeof(RejectJoiningGame),2},
            {typeof(JoinGame),3},
            {typeof(GameMessage),4},
            {typeof(RegisteredGames),5},
            {typeof(GetGamesList),6},
            {typeof(RegisterGame),7},
            {typeof(MoveResponse),8 }

        };
        BoardView1 boardView1;
        Board b = new Board();
        List<MessageProject.Player> listOfPlayers = new List<MessageProject.Player>();
        PlayerLocation coordinates;
        int gameID, playerID;
        Team playerTeam;
        Role playerRole;
        List<GameInfo> gameInfoList = new List<GameInfo>();
        public static Socket master;
        public Form1()
        {
            InitializeComponent();
        }

        private void GetGamesButton_Click(object sender, EventArgs e)
        {


            GetGamesList getgames = new GetGamesList();
            string XMLmessage = MessageProject.Message.messageIntoXML(getgames);

            // SERVER STUFF

            A: Console.Clear();
            Console.Write("Enter host IP address: ");
            string ip = Console.ReadLine();

            Console.Write("Enter port: ");
            Int32 portValue = Convert.ToInt32(Console.ReadLine());
            master = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), 4242);
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), portValue);

            try
            {
                master.Connect(ipe);
            }
            catch
            {
                Console.WriteLine("Could not connect");
                Thread.Sleep(1000);
                goto A;
            }

            Console.Clear();

            Thread t = new Thread(DataIn);
            t.Start();



            byte[] toBytes = Encoding.ASCII.GetBytes(XMLmessage);

            master.Send(toBytes);






        }


        private void JoinButton_Click(object sender, EventArgs e)
        {
            // int index = GetGamesBOX.SelectedIndex;
            
            JoinGame joingame = new JoinGame(1, Role.leader, Team.red);
            string newXMLmessage = MessageProject.Message.messageIntoXML(joingame);
            //  System.Diagnostics.Debug.WriteLine(newXMLmessage);

            // SERVER STUFF

            byte[] toBytes = Encoding.ASCII.GetBytes(newXMLmessage);

            master.Send(toBytes);

        }


        void DataIn()
        {
            byte[] buffer;
            int readBytes;

            while (true)
            {
                try
                {


                    buffer = new byte[master.ReceiveBufferSize];
                    readBytes = master.Receive(buffer);

                    if (readBytes > 0)
                    {

                        string xml = Encoding.ASCII.GetString(buffer);
                        DataManager(xml);
                    }
                }
                catch
                {

                }
            }
        }

        private void DataManager(string xml)
        {
            MessageProject.Message temp = MessageProject.Message.xmlIntoMessage(xml);
            dynamic msg = Convert.ChangeType(temp, temp.GetType());

            switch (typeDict[msg.GetType()])
            {
                case 0:

                    break;
                case 1:

                    playerID = msg.playerID;
                    gameID = msg.gameID;
                    try
                    {
                        playerRole = msg.player.role;
                        playerTeam = msg.player.team;
                    }
                    catch
                    {
                        Console.Write("ERR");
                    }

                    Console.WriteLine("Player id: " + playerID + " gameID: " + gameID + "Player role: " + playerRole + " PLayer team: " + playerTeam);
                    break;

                case 4:
                    b = msg.board;
                    try
                    {
                        listOfPlayers = msg.playerList;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    coordinates = msg.coordinates;
                    Player p = new Player(playerTeam, playerID, gameID,playerRole);
                    p.position_x = coordinates.x;
                    p.position_y = coordinates.y;
                    if (InvokeRequired)
                    {
                        this.Invoke(new Action(() => CreateBoard(b.width,b.taskAreaHeight,b.goalAreaHeight,p)));
                        return;
                    }
                    Console.WriteLine("Goal area height: " + b.goalAreaHeight + "tasj area h: " + b.taskAreaHeight + "Width: " + b.goalAreaHeight);
                    Console.WriteLine("Coordinates: " + coordinates.x + "y: " + coordinates.y);
                    break;
                    
                case 5:
                    gameInfoList.Clear();
                    foreach (GameInfo g in msg.gameInfoList)
                    {
                        // Console.WriteLine("tu:" + g.gameID);
                        try
                        {
                            gameInfoList.Add(g);
                            string s = "ID: " + g.gameID.ToString() + " Blue: " + g.blueTeamPlayers.ToString() + " Red: " + g.redTeamPlayers.ToString();
                            Console.WriteLine(s);

                            if (InvokeRequired)
                            {
                                this.Invoke(new Action(() => RefreshList(s)));
                                return;
                            }
                            
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                    break;

                case 8:
                    Console.WriteLine("OK");
                    try
                    {
                         Console.WriteLine("New player location: X:" + msg.playerLocation.x + "Y: "+msg.playerLocation.y);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    //Console.WriteLine("X: "+ msg.coordinates.x);

                    /* try
                     {

                         boardView1.player.position_x = coordinates.x;
                         boardView1.player.position_y = coordinates.y;
                         boardView1.Refresh();
                     }
                     catch (Exception e)
                     {
                         Console.WriteLine(e);
                     }*/
                    try
                    {
                        if (InvokeRequired)
                        {
                            this.Invoke(new Action(() => UpdateBoard(msg.playerLocation.x, msg.playerLocation.y)));
                            return;
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }

                        break;
                    
                    

                default:
                    break;
            }

        }

        private void RefreshList(string s)
        {
            GetGamesBOX.Items.Clear();
            GetGamesBOX.Items.Add(s);
        }

        private void CreateBoard(int width, int task, int goal, Player p)
        {
            boardView1 = new BoardView1(width, task, goal, p);
            boardView1.Show();
            this.Hide();
        }
        private void UpdateBoard(int x, int y)
        {
            boardView1.player.position_x = x;
            boardView1.player.position_y = y;
            boardView1.UpdateBoard();
            boardView1.Refresh();
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
