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
        private protected double userProvidedElectricityPrice;                              //user provided maximum electricity price he/she wants to run boinc at
        private protected double userProvidedVAT;                                           //user provided VAT in percent or fixed price
        private protected static bool VATPriceType;                                              //If false, then its fixed type price, if true, it is percent type price
        private protected double userProvidedExcise;                                        //user provided excise in percent or fixed price
        private protected static bool excisePriceType;                                      //If false, then its fixed type price, if true, it is percent type price
        private protected List<string> collectedPricesFromUser;

        public double UserProvidedElectricityPrice { get { return userProvidedElectricityPrice; }  set { userProvidedElectricityPrice = value; } }
        public static bool ExcisePriceType { get { return excisePriceType; } }
        public static bool VATtype { get { return excisePriceType; } }

        //public methods
        public void AskElectricityPrice(StreamWriter logWriter)
        {
            while (true)
            {
                try
                {
                    Write(" Please provide baseline electricity price you want to run\n program in megawatts per hour pricing (e.g 45 as in 45€/MWh): ");
                    userProvidedElectricityPrice = double.Parse(ReadLine().Replace(".", ","));
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
        public void AskVAT(StreamWriter logWriter, string parameterToBeAsked)
        {
            AskingBlank(logWriter, ref userProvidedVAT, parameterToBeAsked);
            UserInputedPriceTypeCheck(logWriter, parameterToBeAsked, ref VATPriceType);
        }
        public void AskExcise(StreamWriter logWriter, string parameterToBeAsked)
        {
            AskingBlank(logWriter, ref userProvidedExcise, parameterToBeAsked);
            UserInputedPriceTypeCheck(logWriter, parameterToBeAsked, ref excisePriceType);
            SaveInputToSettingsFile();
        }
        public void SaveInputToSettingsFile()
        {
            Setup setup = new Setup();
            collectedPricesFromUser = new List<string>
            {
                userProvidedElectricityPrice.ToString(),
                userProvidedVAT.ToString(),
                VATPriceType.ToString(),
                userProvidedExcise.ToString(),
                excisePriceType.ToString()
            };
            File.WriteAllLines(setup.SettingsFile, collectedPricesFromUser);
        }
        public void ShowUserProvidedData()
        {
            Clear();
            byte i = 0;
            foreach(string dataPiece in collectedPricesFromUser)
            {
                WriteLine($" {dataPiece} - {(Taxes)i}");
                i++;
            }
            CursorLeft = 1;
        }

        //private methods
        private void AskingBlank(StreamWriter logWriter, ref double price, string _parameterToBeASked)
        {
            while (true)
            {
                try
                {
                    Write($" Please provide {_parameterToBeASked} price for calculating total electricity\n price in your region: ");
                    price = double.Parse(ReadLine().Replace(".", ","));
                    if (price <= 0)
                    {
                        throw new ArgumentException("Zero or negative number user input!");
                    }
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                        $" {DateTime.Now} - User provided {_parameterToBeASked} price or percent: {price}.\n" +
                                        $"                  Saved user provided numerical translation of {_parameterToBeASked} to settings file.");
                    break;
                }
                catch (FormatException)
                {
                    Clear();
                    WriteLine(" Please insert valid number:");
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $"                  User provided {_parameterToBeASked} in incorrect format" +
                                         "                  or there was no input at all.");
                }
                catch (ArgumentException)
                {
                    Clear();
                    WriteLine(" You must provide number that is positive signed number e.g.\n not zero and not with minus sign.");
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $"                  User provided zero or negative {_parameterToBeASked} percent or price.");
                }
            }
            logWriter.Flush();
        }
        private void UserInputedPriceTypeCheck(StreamWriter logWriter, string _parameterToBeAsked, ref bool inputType)
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
                                            $"                  User provided {_parameterToBeAsked} price is in percent.");
                        break;
                    }
                    else if (priceTypeInput.ToLower() == "f")
                    {
                        logWriter.WriteLine($" {DateTime.Now} - -----------------------------\n" +
                                            $"                  User provided {_parameterToBeAsked} price is fixed price.");
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
            Clear();
        }
    }
}