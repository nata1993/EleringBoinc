using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static System.Console;

//calculate spent electricity and its cost
//calculate total electricity used over time.
//check if BOINC is even installed on computer
//optional: send data to database

namespace BoincElectricity
{
    class UserInput
    {
        private protected string priceTypeInput;                                            //parameter for checking if provided prices are either fixed prices or percent
        private protected decimal userProvidedElectricityPrice;                             //user provided maximum electricity price he/she wants to run boinc at
        private protected bool electricityPriceType;
        private protected decimal userProvidedExcise;                                       //user provided excise in percent or fixed price
        private protected bool excisePriceType;
        private protected decimal userProvidedVAT;                                          //user provided VAT in percent or fixed price
        private protected bool VATType;
        private protected string settingsFile = @"C:\BoincElectricity\Boinc-Electricity-User-Settings.txt";
        private protected List<string> combinedPrices;

        public decimal UserProvidedElectricityPrice { get { return userProvidedElectricityPrice; }  set { userProvidedElectricityPrice = value; } }
        public string SettingsFile { get { return settingsFile; } }

        public void AskElectricityPrice(StreamWriter logWriter)
        {
            while (true)
            {
                try
                {
                    Write(" Please provide baseline electricity price you want to run\n program in megawatts per hour pricing (e.g 45 as in 45€/MWh): ");
                    userProvidedElectricityPrice = decimal.Parse(ReadLine().Replace(".", ","));
                    if (userProvidedElectricityPrice <= 0)
                    {
                        throw new ArgumentException("Zero or negative number user input!");
                    }
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                        $" {DateTime.Now} - User provided electricity price: {userProvidedElectricityPrice}.\n" +
                                        $" {DateTime.Now} - User provided numerical translation of electricity price: {userProvidedElectricityPrice}.\n" +
                                        $" {DateTime.Now} - Saved user provided numerical translation of electricity price to settings file.");
                    break;
                }
                catch (FormatException)
                {
                    Clear();
                    WriteLine(" Please insert valid number.\n");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $" {DateTime.Now} - User provided baseline electricity price in incorrect format" +
                                        $" {DateTime.Now} - or there was no input at all.");
                }
                catch (ArgumentException)
                {
                    Clear();
                    WriteLine(" You must provide number that is positive signed number e.g. not zero and not with minus sign.\n");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $" {DateTime.Now} - User provided zero or negative electricity price.");
                }
            }
            logWriter.Flush();
            while(true)
            {
                try
                {
                    WriteLine(" Is it fixed price or percent? Y/N");
                    priceTypeInput = ReadLine();
                    if (priceTypeInput.ToLower() == "y")
                    {
                        electricityPriceType = true;
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - User provided electricity price is in percent.");
                        break;
                    }
                    else if (priceTypeInput.ToLower() == "n")
                    {
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - User provided electricity price is fixed price.");
                        break;
                    }
                    else if (priceTypeInput.ToLower() != "y" || priceTypeInput.ToLower() == string.Empty)
                    {
                        throw new FormatException("Input is not Y or N");
                    }
                }
                catch(FormatException)
                {
                    WriteLine(" Please enter Y or N.");
                }
            }
            logWriter.Flush();
        }
        public void AskExcise(StreamWriter logWriter)
        {
            while (true)
            {
                try
                {
                    Clear();
                    Write(" Please provide Excise price for calculating total electricity\n price in your region: ");
                    userProvidedExcise = decimal.Parse(ReadLine().Replace(".", ","));
                    if (userProvidedExcise <= 0)
                    {
                        throw new ArgumentException("Zero or negative number user input!");
                    }
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                        $" {DateTime.Now} - User provided excise price or percent: {userProvidedExcise}.\n" +
                                        $" {DateTime.Now} - User provided numerical translation of excise: {userProvidedExcise}.\n" +
                                        $" {DateTime.Now} - Saved user provided numerical translation of excise to settings file.");
                    break;
                }
                catch (FormatException)
                {
                    Clear();
                    WriteLine(" Please insert valid number.\n");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $" {DateTime.Now} - User provided excise in incorrect format" +
                                        $" {DateTime.Now} - or there was no input at all.");
                }
                catch (ArgumentException)
                {
                    Clear();
                    WriteLine(" You must provide number that is positive signed number e.g. not zero and not with minus sign.\n");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $" {DateTime.Now} - User provided zero or negative excise percent or price.");
                }
            }
            logWriter.Flush();
            while (true)
            {
                try
                {
                    WriteLine(" Is it fixed price or percent? Y/N");
                    priceTypeInput = ReadLine();
                    if (priceTypeInput.ToLower() == "y")
                    {
                        electricityPriceType = true;
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - User provided excise price is in percent.");
                        break;
                    }
                    else if (priceTypeInput.ToLower() == "n")
                    {
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - User provided excise price is fixed price.");
                        break;
                    }
                    else if (priceTypeInput.ToLower() != "y" || priceTypeInput.ToLower() == string.Empty)
                    {
                        throw new FormatException("Input is not Y or N");
                    }
                }
                catch (FormatException)
                {
                    WriteLine(" Please enter Y or N.");
                }
            }
            logWriter.Flush();
        }
        public void AskVAT(StreamWriter logWriter)
        {
            while (true)
            {
                try
                {
                    Clear();
                    Write(" Please provide VAT price for calculating total electricity\n price in your region: ");
                    userProvidedVAT = decimal.Parse(ReadLine().Replace(".", ","));
                    if (userProvidedVAT <= 0)
                    {
                        throw new ArgumentException("Zero or negative number user input!");
                    }
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                        $" {DateTime.Now} - User provided VAT price or percent: {userProvidedVAT}.\n" +
                                        $" {DateTime.Now} - User provided numerical translation of VAT: {userProvidedVAT}.\n" +
                                        $" {DateTime.Now} - Saved user provided numerical translation of VAT to settings file.");
                    break;
                }
                catch (FormatException)
                {
                    Clear();
                    WriteLine(" Please insert valid number.\n");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $" {DateTime.Now} - User provided VAT in incorrect format" +
                                        $" {DateTime.Now} - or there was no input at all.");
                }
                catch (ArgumentException)
                {
                    Clear();
                    WriteLine(" You must provide number that is positive signed number e.g. not zero and not with minus sign.\n");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $" {DateTime.Now} - User provided zero or negative VAT percent or price.");
                }
            }
            logWriter.Flush();
            while (true)
            {
                try
                {
                    WriteLine(" Is it fixed price or percent? Y/N");
                    priceTypeInput = ReadLine();
                    if (priceTypeInput.ToLower() == "y")
                    {
                        electricityPriceType = true;
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - User provided VAT is in percent.");
                        break;
                    }
                    else if (priceTypeInput.ToLower() == "n")
                    {
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $" {DateTime.Now} - User provided VAT is fixed price.");
                        break;
                    }
                    else if (priceTypeInput.ToLower() != "y" || priceTypeInput.ToLower() == string.Empty)
                    {
                        throw new FormatException("Input is not Y or N");
                    }
                }
                catch (FormatException)
                {
                    WriteLine(" Please enter Y or N.");
                }
            }
            logWriter.Flush();
        }
        public void ShowUserProvidedData()
        {
            Clear();
            byte i = 0;
            foreach(string dataPiece in combinedPrices)
            {
                WriteLine($"{dataPiece} - {(Taxes)i}");
                i++;
            }
            Thread.Sleep(5000);
        }
        public void SaveInputToSettingsFile()
        {
            combinedPrices = new List<string>();
            combinedPrices.Add(userProvidedElectricityPrice.ToString());
            combinedPrices.Add(userProvidedExcise.ToString());
            combinedPrices.Add(userProvidedVAT.ToString());
            File.WriteAllLines(settingsFile, combinedPrices);
        }
        private void CombinePrices()
        {

        }
    }
}