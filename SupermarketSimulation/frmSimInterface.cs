﻿//////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//	Project:	    Project 4
//	File Name:		frmSimInterface.cs
//	Description:    Gui for the supermarket simulation
//	Course:			CSCI 2210-001 - Data Structures
//	Author:			Chance Reichenberg, reichenberg@etsu.edu, Duncan Perkins, perkinsdt@goldmail.etsu.edu, Department of Computing, East Tennessee State University
//	Created:	    Friday, April 10, 2015
//	Copyright:		Duncan Perkins, Chance Reichenberg, 2015
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PriorityQueueUtility;

namespace SupermarketSimulation
{
    /// <summary>
    /// Gui for the SuperMarket Simulation
    /// </summary>
    public partial class frmSimInterface : Form
    {
        Random rand = new Random();
        //Priority Queue to manage events
        PriorityQueue<Event> PQ = new PriorityQueue<Event>();
        //List of queues to represent registers
        List<Queue<Customer>> registers;

        //Variables to base simulation on
        int numCustomers = 600;
        int hoursOfOperation = 16;
        int numRegisters = 5;
        int expectedCheckoutDurationMin = 6;
        int expectedCheckoutDurationSeconds = 15;
        int TimeInterval;
        int expectedMinimumMin, expectedMinimumSeconds;

        //Statistics variables
        int eventsProcessed, arrivals, departures;
        int longestQueue = 0;
        TimeSpan shortestServiceTime, longestServiceTime, averageServiceTime, totalServiceTime;
        
        

        /// <summary>
        /// Default constructor for the form
        /// </summary>
        public frmSimInterface()
        {
            InitializeComponent();
            TimeInterval = 1100;
        }

        /// <summary>
        /// Button click event method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnRun_Click(object sender, EventArgs e)
        {
            try
            {
                //Make sure none of the fields are empty
                if (String.IsNullOrEmpty(txtMinimumMinutes.Text) || String.IsNullOrEmpty(txtMinimumSeconds.Text) || String.IsNullOrEmpty(txtCustomers.Text) || String.IsNullOrEmpty(txtHours.Text) || String.IsNullOrEmpty(txtRegisters.Text) || String.IsNullOrEmpty(txtCheckoutDurationMinutes.Text) || String.IsNullOrEmpty(txtCheckoutDurationSeconds.Text))
                {
                    MessageBox.Show("All fields must be filled. Please enter data into the fields.");
                    return;

                }
                else if (Double.Parse(txtCustomers.Text) < 1 || int.Parse(txtHours.Text) < 1 || int.Parse(txtRegisters.Text) < 1 || int.Parse(txtCheckoutDurationMinutes.Text) < 1) //Some fields cannot be 0
                {
                    MessageBox.Show("Fields other than 'Expected Minimum Checkout Duration' Cannot be '0'.");
                    return;
                }
                else
                {
                    ToggleControls();
                    ResetStats();

                    numCustomers = PoissonNum(Double.Parse(txtCustomers.Text));
                    hoursOfOperation = int.Parse(txtHours.Text);
                    numRegisters = int.Parse(txtRegisters.Text);
                    
                    expectedCheckoutDurationMin = int.Parse(txtCheckoutDurationMinutes.Text);
                    expectedCheckoutDurationSeconds = int.Parse(txtCheckoutDurationSeconds.Text);
                    expectedMinimumMin = int.Parse(txtMinimumMinutes.Text);
                    expectedMinimumSeconds = int.Parse(txtMinimumSeconds.Text);

                    pgbSimulationProgress.Maximum = numCustomers;
                    
                }

                registers = new List<Queue<Customer>>();

                //Adds a Queue to the list for each register
                for (int count = 0; count < numRegisters; count++)
                {
                    registers.Add(new Queue<Customer>());
                }
                GenerateCustomerArrivals();
                int n = await RunSimulation();
            }
            catch( Exception err)
            {
                MessageBox.Show("There was an error processing your simulation.\n" + err.Message + "\n", "Error",MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
             
        }


        /// <summary>
        /// Negative exponential distribution generator method
        /// </summary>
        /// <param name="expectedValue">Value used to generate Negative exponential value</param>
        /// <returns>Negative exponential number</returns>
        private double NegExponentialNum(double expectedValue)
        {
            return -expectedValue * Math.Log(rand.NextDouble());
        }

        /// <summary>
        /// Poisson distribution number generator method
        /// </summary>
        /// <param name="expectedValue">value used to generate the numbers around</param>
        /// <returns>poisson distribution value</returns>
        private int PoissonNum(double expectedValue)
        {
            double dLimit = -expectedValue;
            double dSum = Math.Log(rand.NextDouble());

            int count;
            for(count = 0; dSum > dLimit; count++)
            {
                dSum += Math.Log(rand.NextDouble());
            }
            return count;
        }

        /// <summary>
        /// Method to generate customer events
        /// </summary>
        private void GenerateCustomerArrivals()
        {
            for(int i = 0; i < numCustomers; i++)
            {
                Customer tempCust = new Customer(i + 1, new TimeSpan(0, 0, rand.Next((hoursOfOperation * 60) * 60)), new TimeSpan());
                PQ.Enqueue(new Event(EVENTTYPE.ENTER, tempCust, tempCust.ArrivalTime));
            }
        }

        /// <summary>
        /// Method that runs the Supermarket Simulation
        /// </summary>
        private async Task<int> RunSimulation()
        {
            //Set the stats variables so that they will most certainly be overwritten
            shortestServiceTime = new TimeSpan(1,0,0);
            longestServiceTime = new TimeSpan(0,0,0);
            while(PQ.Count != 0)
            {
                int shortestLineIndex = 0;
                int lineLength = registers[0].Count;
                Event tempEvent = PQ.Peek();

                //Handles the enter event  by adding the person to the shortest line 
                //and creating an exit event based on the time they have to wait to exit
                if(tempEvent.Type == EVENTTYPE.ENTER)
                {
                    //Gets the index of the shortest line to be checked out
                    for(int i = 0; i < registers.Count; i++)
                    {
                        if(registers[i].Count < lineLength)
                        {
                            lineLength = registers[i].Count;
                            shortestLineIndex = i;
                        }
                        

                        //shortestLineIndex = (registers[i].Count < lineLength) ? i : shortestLineIndex;
                    }

                    registers[shortestLineIndex].Enqueue(tempEvent.Customer);               //Add the customer to the shortest line

                    PQ.Dequeue();       //Then remove that customer's arrival from the priority Queue

                    if(registers[shortestLineIndex].Peek().CustomerID == tempEvent.Customer.CustomerID)
                    {
                        SetCustomerTimeToServe(registers[shortestLineIndex].Peek());

                    }
                    
                    arrivals++;         //Increment Arrivals
                    lblArrivals.Text = String.Format("Arrivals: {0}", arrivals.ToString());
                }

                //Handles the exit event by finding where the person is and removing them from their line
                if(tempEvent.Type == EVENTTYPE.LEAVE)
                {
                    for(int count = 0; count < registers.Count; count++)
                        if (registers[count].Count > 0)
                        {
                            if (registers[count].Peek().CustomerID == tempEvent.Customer.CustomerID)
                            {
                                
                                registers[count].Dequeue();     //Remove the customer from that line

                                PQ.Dequeue();       //Then remove that customer's Exit from the priority Queue

                                if (registers[count].Count > 0)
                                {
                                    if (registers[count].Peek().TimeToServe == new TimeSpan())
                                    {
                                        SetCustomerTimeToServe(registers[count].Peek());

                                    }
                                }


                                departures++;       //Increment departures
                                pgbSimulationProgress.Value = departures;
                                lblDepartures.Text = String.Format("Departures: {0}", departures.ToString());

                                break;
                            }
                        }
                    
                }
                
                eventsProcessed++;
                lblEvents.Text = String.Format("Events Processed: {0}",eventsProcessed.ToString());
                GetLongestQueue();
                int n = await VisualizeData();
            }

            averageServiceTime = new TimeSpan(0, 0, (int)(totalServiceTime.TotalSeconds / numCustomers));
            lblAvgWait.Text = String.Format("Average Wait Time: {0}", averageServiceTime.ToString());
            lblShortestWait.Text = String.Format("Shortest Wait: {0}", shortestServiceTime.ToString());
            lblLongestWait.Text = String.Format("Longest Wait: {0}", longestServiceTime.ToString());
            ToggleControls();
            return 1;
        }

        /// <summary>                                                
        /// Creates the Psuedo-Graphical View of the simulation      
        /// </summary>                                               
        /// <returns>1 for done</returns>                            
        public async Task<int> VisualizeData()
        {
            //Final string to be displayed in the GUI
            string str = "";

            //Temporary string for use in the methods
            string tempstr = "";

            //index of the longest current register
            int biggest = 0;

            //Copy of the register List
            List<Queue<Customer>> RegisterCopy = new List<Queue<Customer>>();
            int StringLength = 7;

            //Deep copy the registers queue 
            foreach (Queue<Customer> q in registers)
            {
                RegisterCopy.Add(new Queue<Customer>(q.ToList()));
            }

            //Where the "Registers" title should be placed on the screen
            int TitleSpot = ((registers.Count*StringLength) / 2) - 4;

            //Do the placing
            for (int i = 0; i < TitleSpot; i++)
            {
                str += " ";
            }
            str += "Registers\r\n";

            //Print line of dashes for readability
            for (int i = 0; i < registers.Count * StringLength; i++)
            {
                str += "-";
            }
            str += "\r\n";

            //variable for number of registers inputted by user
            int RegisterIndex = 0;

            //print out Register lines
            for (int i = 0; i < registers.Count; i++)
            {
                string SpacesString = "";
                tempstr = "R " + i  ;
                if (tempstr.Length < StringLength)
                {
                    int NewSpaces = (StringLength - tempstr.Length);
                    for (int k = 0; k < NewSpaces-1; k++)
                    {
                        SpacesString += " ";
                    }
                    SpacesString += "|";
 
                }
                tempstr += SpacesString;
                str += tempstr;
                RegisterIndex++;
            }

            //Add another line of dashes for readability
            str += "\r\n";
            for (int i = 0; i < RegisterIndex*StringLength; i++)
            {
                str += "-";
            }

                str += "\r\n";

            //Determine the longest Register
            for (int j = 0; j < registers.Count; j++)
            {
                if (registers[j].Count-1 > registers[biggest].Count-1)
                {
                    biggest = j;
                }
            }

            //Print out either the customer ID of the current place in line,
            //Or print out spaces if no one is in line at the current position
            for (int l = 0; l < registers[biggest].Count; l++)
            {
                foreach (Queue<Customer> q in RegisterCopy)
                {
                    //If there are customers in the queue 
                    if (q.Count > 0)
                    {
                        string SpacesString = "";
                        tempstr = q.Dequeue().CustomerID.ToString();
                        if (tempstr.Length < StringLength)
                        {
                            
                            int NewSpaces = (StringLength - tempstr.Length);
                            for (int i = 0; i < NewSpaces-1; i++)
                            {
                                SpacesString += " ";
                            }
                            SpacesString += "|";
                        }
                        tempstr += SpacesString;
                        str+=tempstr;

                    }

                    //if there aren't any customers in the queue 
                    else
                    {
                        for (int i = 0; i < StringLength-1; i++)
                        {
                            str += " ";
                        }
                        str += "|";

                    }

                }
                str += "\r\n";
            }
            //update GUI string
            txtSimulationVisual.Text = str;
            //Delay so the gui isn't instantly at the end
            int n = await Wait();

            
            return 1;
        }

        /// <summary>
        /// Validates data entered into text boxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyValidation_KeyPress(object sender, KeyPressEventArgs e)
        {
            //If the base is less than 10 only allow characters that are less than the base to be entered. If the base is 10 or greater allow 0-9 to be entered
            if ((e.KeyChar >= '0' && e.KeyChar <= '9') || e.KeyChar == (char)Keys.Back)
            {
                return;
            }
            else
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the close button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Method that loops through the registers and sets the longestQueue to the longest current queue;
        /// </summary>
        private void GetLongestQueue()
        {
            foreach(var line in registers)
            {
                longestQueue = (line.Count > longestQueue) ? line.Count : longestQueue;
            }

            lblLongestQueue.Text = String.Format("Longest Queue Encountered: {0}", longestQueue.ToString());
            
        }

        /// <summary>
        /// Resets Statistics and stats labels
        /// </summary>
        private void ResetStats()
        {
            arrivals = 0;
            lblArrivals.Text = "Arrivals: ";
            departures = 0;
            lblDepartures.Text = "Departures: ";
            eventsProcessed = 0;
            lblEvents.Text = "Events Processed: ";
            longestQueue = 0;
            lblLongestQueue.Text = "Longest Queue Encountered: ";
            shortestServiceTime = new TimeSpan();
            longestServiceTime = new TimeSpan();
            averageServiceTime = new TimeSpan();
            totalServiceTime = new TimeSpan();
            lblAvgWait.Text = "Average Wait Time: ";
            lblShortestWait.Text = "Shortest Wait: " ;
            lblLongestWait.Text = "Longest Wait: ";

        }
        
        /// <summary>
        /// Sets the time interval for the simulation
        /// </summary>
        /// <returns>1 for done</returns>
        public async Task<int> Wait()
        {
            await Task.Delay(TimeInterval);
            return 1;
        }

        /// <summary>
        /// Method for the Slider scroll event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimulationSpeed_Scroll(object sender, EventArgs e)
        {
            TimeInterval = (100 - SimulationSpeed.Value) * 100;
            lblSpeed.Text =  TimeInterval.ToString() + " Millisecond Delay";

        }

        /// <summary>
        /// Method to give a customer a Time taken to be served, and adds their exit event to the Priority Queue
        /// </summary>
        /// <param name="cust">Customer to be given a Time to be served and added to the Priority Queue</param>
        private void SetCustomerTimeToServe(Customer cust)
        {
            //Calculate Time to Serve using a negative exponential formula with a minimum value
            cust.TimeToServe = new TimeSpan(0, 0, (int)((expectedMinimumMin * 60 + expectedMinimumSeconds) + NegExponentialNum((expectedCheckoutDurationSeconds + (expectedCheckoutDurationMin * 60)) - (expectedMinimumMin * 60 + expectedMinimumSeconds))));

            TimeSpan custExitTime = cust.ArrivalTime + cust.TimeToServe;

            //If the customer's wait time is less than the shortest wait time, then set it to be the shortest wait time.
            shortestServiceTime = (cust.TimeToServe < shortestServiceTime) ? cust.TimeToServe : shortestServiceTime;
            //If the customer's wait time is longer than the longest wait time, then set it to be the longest wait time.
            longestServiceTime = (cust.TimeToServe > longestServiceTime) ? cust.TimeToServe : longestServiceTime;
            //Add the customer's wait time to the total service time
            totalServiceTime += cust.TimeToServe;
            PQ.Enqueue(new Event(EVENTTYPE.LEAVE, cust, custExitTime));
        }

        /// <summary>
        /// Toggles the Enabled property for the user controls
        /// </summary>
        private void ToggleControls()
        {
            btnRun.Enabled = !btnRun.Enabled;
            txtCheckoutDurationMinutes.Enabled = !txtCheckoutDurationMinutes.Enabled;
            txtCheckoutDurationSeconds.Enabled = !txtCheckoutDurationSeconds.Enabled;
            txtCustomers.Enabled = !txtCustomers.Enabled;
            txtHours.Enabled = !txtHours.Enabled;
            txtRegisters.Enabled = !txtRegisters.Enabled;
            txtMinimumMinutes.Enabled = !txtMinimumMinutes.Enabled;
            txtMinimumSeconds.Enabled = !txtMinimumSeconds.Enabled;
        }

        /// <summary>
        /// Event method to handle form closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmSimInterface_FormClosed(object sender, FormClosedEventArgs e)
        {
            MessageBox.Show("Thank you for using the SuperMarket Simulator.", "GoodBye!", MessageBoxButtons.OK);

        }




    
    
    }

}
