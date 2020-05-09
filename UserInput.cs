using System;
using System.Collections.Generic;
using System.IO;
using static System.Console;

namespace BoincElectricity
{
    //Class for asking from used some info on the first program start-up 
    //or when settings file is deleted/corrupted
    class UserInput
    {
        private protected string priceTypeInput;                                            //parameter for checking if provided prices are either fixed prices or percent
        private protected double userProvidedElectricityPrice;                              //user provided maximum electricity price he/she wants to run boinc at
        private protected double userProvidedVAT;                                           //user provided VAT in percent or fixed price
        private protected static bool VATPriceType;                                         //If false, then its fixed type price, if true, it is percent type price
        private protected double userProvidedExcise;                                        //user provided excise in percent or fixed price
        private protected static bool excisePriceType;                                      //If false, then its fixed type price, if true, it is percent type price
        private protected List<string> collectedPricesFromUser;

        public double UserProvidedElectricityPrice { get { return userProvidedElectricityPrice; }  set { userProvidedElectricityPrice = value; } }
        public static bool ExcisePriceType { get { return excisePriceType; } }
        public static bool VATtype { get { return excisePriceType; } }

        //public methods
        public void AskUserInput(StreamWriter logWriter)
        {
            AskElectricityPrice(logWriter);
            AskVAT(logWriter, "VAT");
            AskExcise(logWriter, "Excise");
            SaveInputToSettingsFile();
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
                    logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                        $" {DateTime.Now} - User provided {_parameterToBeASked} price or percent: {price}.\n" +
                                        $"                       Saved user provided numerical translation of {_parameterToBeASked} to settings file.");
                    break;
                }
                catch (FormatException)
                {
                    Clear();
                    WriteLine(" Please insert valid number:");
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $"                       User provided {_parameterToBeASked} in incorrect format\n" +
                                         "                       or there was no input at all.");
                }
                catch (ArgumentException)
                {
                    Clear();
                    WriteLine(" You must provide number that is positive signed number e.g.\n not zero and not with minus sign.");
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                        $"                       User provided zero or negative {_parameterToBeASked} percent or price.");
                }
            }
            logWriter.Flush();
        }
        private void AskElectricityPrice(StreamWriter logWriter)
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
                    logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                        $"                       User provided electricity price: {userProvidedElectricityPrice}.\n" +
                                         "                       Saved user provided numerical translation of electricity price to settings file.");
                    break;
                }
                catch (FormatException)
                {
                    Clear();
                    WriteLine(" Please insert valid number.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                       User provided baseline electricity price in incorrect format\n" +
                                         "                       or there was no input at all.");
                }
                catch (ArgumentException)
                {
                    Clear();
                    WriteLine(" You must provide number that is positive signed number e.g.\n not zero and not with minus sign.");
                    //log
                    logWriter.WriteLine($" {DateTime.Now} - !!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                                         "                       User provided zero or negative electricity price.");
                }
            }
            logWriter.Flush();
            Clear();
        }
        private void AskVAT(StreamWriter logWriter, string parameterToBeAsked)
        {
            AskingBlank(logWriter, ref userProvidedVAT, parameterToBeAsked);
            UserInputedPriceTypeCheck(logWriter, parameterToBeAsked, ref VATPriceType);
        }
        private void AskExcise(StreamWriter logWriter, string parameterToBeAsked)
        {
            AskingBlank(logWriter, ref userProvidedExcise, parameterToBeAsked);
            UserInputedPriceTypeCheck(logWriter, parameterToBeAsked, ref excisePriceType);
            SaveInputToSettingsFile();
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
                        logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                            $"                       User provided {_parameterToBeAsked} price is in percent.");
                        break;
                    }
                    else if (priceTypeInput.ToLower() == "f")
                    {
                        logWriter.WriteLine($" {DateTime.Now} - --------------------------\n" +
                                            $"                       User provided {_parameterToBeAsked} price is fixed price.");
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
        private void SaveInputToSettingsFile()
        {
            collectedPricesFromUser = new List<string>
            {
                Setup.BoincInstallationPath,
                userProvidedElectricityPrice.ToString(),
                userProvidedVAT.ToString(),
                VATPriceType.ToString(),
                userProvidedExcise.ToString(),
                excisePriceType.ToString(),
            };
            File.WriteAllLines(Setup.SettingsFile, collectedPricesFromUser);
        }
    }
}