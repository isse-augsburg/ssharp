// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

// run with:
// spin645.exe -a "Height Control.spin"
// gcc.exe -o pan pan.c -O3 -DSAFETY 
// pan.exe -m1000000 -X -w27

#define PreControlPos 3
#define MainControlPos 6
#define EndControlPos 9
#define TunnelPos 12
#define Timeout 4
#define Left 0
#define Right 1

hidden byte lb1mis;
hidden byte lb1false;
hidden byte lb2mis;
hidden byte lb2false;
hidden byte odlmis;
hidden byte odlfalse;
hidden byte odrmis;
hidden byte odrfalse;
hidden byte odfmis;
hidden byte odffalse;

#define LB1 (!lb1mis & (lb1false | (pos1 - speed1 <= PreControlPos & pos1 > PreControlPos) | (pos2 - speed2 <= PreControlPos & pos2 > PreControlPos)))
#define LB2 (!lb2mis & (lb2false | (pos1 - speed1 <= MainControlPos & pos1 > MainControlPos) | (pos2 - speed2 <= MainControlPos & pos2 > MainControlPos)))
#define ODL (!odlmis & (odlfalse | (pos1 - speed1 <= MainControlPos & pos1 > MainControlPos & lane1 == Left) | (pos2 - speed2 <= MainControlPos & pos2 > MainControlPos & lane2 == Left) | (pos3 - speed3 <= MainControlPos & pos3 > MainControlPos & lane3 == Left)))
#define ODR (!odrmis & (odrfalse | (pos1 - speed1 <= MainControlPos & pos1 > MainControlPos & lane1 == Right) | (pos2 - speed2 <= MainControlPos & pos2 > MainControlPos & lane2 == Right) | (pos3 - speed3 <= MainControlPos & pos3 > MainControlPos & lane3 == Right)))
#define ODF (!odfmis & (odffalse | (pos1 - speed1 <= EndControlPos & pos1 > EndControlPos & lane1 == Left) | (pos2 - speed2 <= EndControlPos & pos2 > EndControlPos & lane2 == Left) | (pos3 - speed3 <= EndControlPos & pos3 > EndControlPos & lane3 == Left)))

bool tunnelClosed;

byte pos1;
byte pos2;
byte pos3;
hidden byte speed1;
hidden byte speed2;
hidden byte speed3;
byte lane1;
byte lane2;
byte lane3;

short mainTime;
byte mainCount;
short endTime;
hidden byte mainActive;
hidden byte mainOnlyRightTriggered;
hidden byte mainLeavingLeft;
hidden byte mainLeavingRight;

active proctype System() 
{
	atomic
	{
		pos1 = 0
		pos2 = 0
		pos3 = 0
		tunnelClosed = false
		lane1 = Right
		lane2 = Right
		lane3 = Right
		speed1 = 0
		speed2 = 0
		speed3 = 0
		mainTime = -1
		mainCount = 0
		endTime = -1
	};
	
	do 
		:: atomic
		{	
			if 
				:: !tunnelClosed;
					if :: pos1 < EndControlPos; speed1 = 1 :: true; speed1 = 2 fi;
					if :: pos2 < EndControlPos; speed2 = 1 :: true; speed2 = 2 fi;
					if :: pos3 < EndControlPos; speed3 = 1 :: true; speed3 = 2 fi;
					
					d_step
					{
						pos1 = ((pos1 + speed1 < TunnelPos) -> (pos1 + speed1) : TunnelPos)
						pos2 = ((pos2 + speed2 < TunnelPos) -> (pos2 + speed2) : TunnelPos)
						pos3 = ((pos3 + speed3 < TunnelPos) -> (pos3 + speed3) : TunnelPos)
					};
					
					if :: pos1 < EndControlPos; lane1 = Left :: pos1 < EndControlPos; lane1 = Right :: else; skip fi;
					if :: pos2 < EndControlPos; lane2 = Left :: pos2 < EndControlPos; lane2 = Right :: else; skip fi;
					if :: pos3 < EndControlPos; lane3 = Left :: pos3 < EndControlPos; lane3 = Right :: else; skip fi;
				:: else; skip
			fi;
			
			mainTime = (mainTime > -1 -> mainTime - 1 : -1)
						
			if :: true; lb1mis = 0 :: true; lb1mis = 1 fi;
			if :: true; lb1false = 0 :: true; lb1false = 1 fi;
			
			d_step
			{
				if
					:: LB1; 
						mainTime = Timeout
						mainCount = mainCount + 1
					:: else; skip
				fi;
			};
									
			mainActive = mainCount != 0
			
			if
				:: mainActive;
					if :: true; lb2mis = 0 :: true; lb2mis = 1 fi;
					if :: true; lb2false = 0 :: true; lb2false = 1 fi;
					if :: true; odlmis = 0 :: true; odlmis = 1 fi;
					if :: true; odlfalse = 0 :: true; odlfalse = 1 fi;
					if :: true; odrmis = 0 :: true; odrmis = 1 fi;
					if :: true; odrfalse = 0 :: true; odrfalse = 1 fi;
				:: else; skip
			fi;
			
			d_step
			{
				mainOnlyRightTriggered = mainActive & !ODL & ODR
				mainLeavingLeft = mainActive & !mainOnlyRightTriggered & LB2
				mainLeavingRight = mainActive & mainOnlyRightTriggered & LB2
				
				if
					:: mainLeavingLeft; mainCount = mainCount - 1
					:: else; skip		
				fi;
				
				if
					:: mainLeavingRight; mainCount = mainCount - 1
					:: else; skip		
				fi;
				
				if
					:: mainTime == 0; mainCount = 0
					:: else; skip
				fi;
				
				if
					:: mainCount == 0; mainTime = -1
					:: else; skip
				fi;
				
				mainCount = (mainCount < 0 -> 0 : (mainCount > 5 -> 5 : mainCount))
				endTime = (endTime > -1 -> endTime - 1 : -1)
				
				if
					:: mainLeavingRight; endTime = Timeout
					:: else; skip
				fi;
			};
			
			if
				:: endTime > 0;
					if :: true; odfmis = 0 :: true; odfmis = 1 fi;
					if :: true; odffalse = 0 :: true; odffalse = 1 fi;
				:: else; skip
			fi;
			
			if
				:: mainLeavingLeft | (endTime > 0 & ODF); tunnelClosed = true
				:: else; skip
			fi;
		};
	od
}