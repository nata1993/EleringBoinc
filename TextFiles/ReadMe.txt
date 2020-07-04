Creator: Bogdan Parubok
Country: Estonia
Contact: bogdan.parubok@hotmail.com

======

BoincElectricity is a console program that is created for single purpose - optimize electricity spending.
It is done by calling local electricity provider API, acquiring data from called API and using that data
to reference to user provided maximum electricity price a person wants to run BOINC program at.
BOINC is a platform for various projects with which a person can contribute home computer or laptop or any
other compactible device computing power for science. One of such projects is WorldCommunityGrid which found
during latest pandemic event, the spread of SARS-COV-2 virus, the virus protein properties for mechanism of
attaching itself to the hosts cells by using special receptors.
Running BOINC software utilizes computer ressources, namely CPU and GPU computing power to solve complex
mathematical calculations for science. In essence combining any BOINC program user computed data, we get world
wide grid of computers which all work for one goal - advancing science through virtual problem solving.
This means that grid of computers basically comes together as one supercomputer. However, giving unspent
computer ressources for science spends electricity and electricity has a price tag. Hence this BoincElectricity
program was created.
This program asks during first start-up some data from user which will be used for basic program work. That data
will be saved locally in .txt file format for user to easily read what was asked from user.
For user input there will be used: the maximum electricity price limit, the local excise on electricity and the
local VAT.
The maximum electricity price limit will be used as reference point for starting BOINC program if local electricity
price is below user provided limit as well as shutting down BOINC program when electricity price rises above user
provided price limit.
The local excise on electricity will be used only for calculating total price per kilowatthour (kWh).
The local VAT is same as with excise with only goal for calculating total price per kilowatthour.
This is done by combining local electricity price, excise and VAT into one final pricetag that user will be paying
for each additional kWh that BOINC program will be using for calculating data for science.
BoincElectricity however will be using only local electricity price as comparing value to user provided maximum
electricity price limit. This is done this way because powerplants sell electricity to stockmarket of electricity
as tax-free. But the taxes and excise are added after the electricity has been sold - the usual basic economy stuff.
BoincElectricity will be running automatically after user provided necessary data: first it will call local
electricity provider API, gathered data from API will be shown in console, if price is below user provided limit
BOINC is started, if it is above, BoincElectricity will wait until next o'clock and call API again. This loop
continues until local electricity price falls below limit at which point BOINC program is started.
When BOINC was started, BoincElectricity will wait till next o'clock and then call API. If went above limit, BOINC
is shut down, if price did not rise above limit, BOINC is not shut down and BoincElectricity will again wait till
next o'clock. This loop continues until user closes BoincElectricity program. Closing BoincElectricity will not close
BOINC program by itself hence BOINC will continue running until user closes that program too, otherwise price following
BoincElectricity wont be regulating when BOINC must work and must stop working which in turn will let BOINC to run at
some point at unfavorable electricity price for user.
BoincElectricity is simple and lightweight program written in C# for Windows operating system. At some point in future
BoincElectricity will also support Linux operating system too.
This program main auditory is for those users who dont have big money on hand to spend right and left but still want to
contribute somehow to science. BOINC is one of such ways and BoincElectricity was created for this purpose to attempt to
solve such problem.

======
Notes
______
1) Price on electricity stockmarket is usually in MegaWattHour pricing. User usually consumes in KiloWattHour pricing.
   BoincElectricity is showing and calculating pricing in MegaWattHours. If you want to know how much you pay for each
   KiloWattHour, then simply divide MegaWattHour by 1000 e.g 20 €/Mwh / 1000 = 0.02 €/kWh or 2 cents per kwh.
2) BoincElectricity must be started before BOINC is started, otherwise program crashes because it is unable to parse
   BOINC processes under itself to control BOINC shutting down and restarting process in a loop.