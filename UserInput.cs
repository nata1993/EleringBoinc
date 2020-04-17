using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
        private protected decimal userProvidedExcise;                                       //user provided excise in percent or fixed price
        private protected static bool excisePriceType = false;                              //If false, then its fixed type price, if true, it is percent type price
        private protected decimal userProvidedVAT;                                          //user provided VAT in percent or fixed price
        private protected static bool vatType = false;                                      //If false, then its fixed type price, if true, it is percent type price
        private protected List<string> collectedPrices;

        public decimal UserProvidedElectricityPrice { get { return userProvidedElectricityPrice; }  set { userProvidedElectricityPrice = value; } }
        public static bool ExcisePriceType { get { return excisePriceType; } }
        public static bool VATtype { get { return excisePriceType; } }

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
                                        $"                  User provided electricity price: {userProvidedElectricityPrice}.\n" +
                                        $"                  User provided numerical translation of electricity price: {userProvidedElectricityPrice}.\n" +
                                         "                  Saved user provided numerical translation of electricity price to settings file.");
                    break;
                }
                catch (FormatException)
                {
                    Clear();
                    WriteLine(" Please insert valid number.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                  User provided baseline electricity price in incorrect format" +
                                         "                  or there was no input at all.");
                }
                catch (ArgumentException)
                {
                    Clear();
                    WriteLine(" You must provide number that is positive signed number e.g.\n not zero and not with minus sign.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                  User provided zero or negative electricity price.");
                }
            }
            logWriter.Flush();
            Clear();
        }
        public void AskVAT(StreamWriter logWriter)
        {
            while (true)
            {
                try
                {
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
                                         "                  Saved user provided numerical translation of VAT to settings file.");
                    break;
                }
                catch (FormatException)
                {
                    Clear();
                    WriteLine(" Please insert valid number.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                  User provided VAT in incorrect format" +
                                         "                  or there was no input at all.");
                }
                catch (ArgumentException)
                {
                    Clear();
                    WriteLine(" You must provide number that is positive signed number e.g.\n not zero and not with minus sign.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                  User provided zero or negative VAT percent or price.");
                }
            }
            logWriter.Flush();
            UserInputedPriceTypeCheck(logWriter, "VAT", ref vatType);
            Clear();
        }
        public void AskExcise(StreamWriter logWriter)
        {
            while (true)
            {
                try
                {
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
                                         "                  Saved user provided numerical translation of excise to settings file.");
                    break;
                }
                catch (FormatException)
                {
                    Clear();
                    WriteLine(" Please insert valid number.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                  User provided excise in incorrect format" +
                                         "                  or there was no input at all.");
                }
                catch (ArgumentException)
                {
                    Clear();
                    WriteLine(" You must provide number that is positive signed number e.g.\n not zero and not with minus sign.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                  User provided zero or negative excise percent or price.");
                }
            }
            logWriter.Flush();
            UserInputedPriceTypeCheck(logWriter, "excise", ref excisePriceType);
            Clear();
        }
        private void UserInputedPriceTypeCheck(StreamWriter logWriter, string userInput, ref bool inputType)
        {
            while (true)
            {
                try
                {
                    WriteLine(" Is it fixed price or percent? F = fixed || P = percent");
                    CursorLeft = 1;
                    priceTypeInput = ReadLine();
                    if (priceTypeInput.ToLower() == "p")
                    {
                        inputType = true;
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $"                  User provided {userInput} price is in percent.");
                        break;
                    }
                    else if (priceTypeInput.ToLower() == "f")
                    {
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $"                  User provided {userInput} price is fixed price.");
                        break;
                    }
                    else
                    {
                        throw new FormatException("Input is not F or P");
                    }
                }
                catch (FormatException)
                {
                    WriteLine(" Please enter \"F\" for fixed or \"P\" for percent.");
                }
            }
            logWriter.Flush();
        }
        public void ShowUserProvidedData()
        {
            Clear();
            byte i = 0;
            foreach(string dataPiece in collectedPrices)
            {
                WriteLine($" {dataPiece} - {(Taxes)i}");
                i++;
            }
            CursorLeft = 1;
            Task.Delay(5000);
        }
        public void SaveInputToSettingsFile()
        {
            Setup setup = new Setup();
            collectedPrices = new List<string>
            {
                userProvidedElectricityPrice.ToString(),
                userProvidedExcise.ToString(),
                userProvidedVAT.ToString()
            };
            File.WriteAllLines(setup.SettingsFile, collectedPrices);
        }
    }
}