       01 SA-CID-ASSN.
          05 SA-CID-ASSN-C.   
             10 SACID-SA-CID-SELL-ASSN.                               
                15 SACIDASN-I-BAS-SELL               PIC S9(8)         
                                                     VALUE ZERO
                                                     COMPUTATIONAL.
                15 SACIDASN-CUSTOMER-NBR             PIC S9(10) COMP-3
                                                     VALUE ZERO.
                15 SACIDASN-D-SELL-ASGN-EFF          PIC X(10)         
                                                     VALUE SPACE.    
                15 SACIDASN-D-SELL-ASGN-EFF-FULL REDEFINES                    
                     SACIDASN-D-SELL-ASGN-EFF.                           
                   20 SACIDASN-D-SELL-ASGN-EFF-MO-X.                      
                      25 SACIDASN-D-SELL-ASGN-EFF-MO PIC 9(2).         
                   20 SACIDASN-D-SELL-ASGN-EFF-S1    PIC X(1).         
                   20 SACIDASN-D-SELL-ASGN-EFF-DY-X.                      
                      25 SACIDASN-D-SELL-ASGN-EFF-DY PIC 9(2).         
                   20 SACIDASN-D-SELL-ASGN-EFF-S2    PIC X(1).         
                   20 SACIDASN-D-SELL-ASGN-EFF-YR-X.                      
                      25 SACIDASN-D-SELL-ASGN-EFF-YR PIC 9(4).         
                15 SACIDASN-D-SELL-ASGN-EXP          PIC X(10)         
                                                     VALUE SPACE.
                15 SACIDASN-D-SELL-ASGN-EXP-FULL REDEFINES            
                      SACIDASN-D-SELL-ASGN-EXP.                           
                   20 SACIDASN-D-SELL-ASGN-EXP-MO-X.                      
                      25 SACIDASN-D-SELL-ASGN-EXP-MO PIC 9(2).         
                   20 SACIDASN-D-SELL-ASGN-EXP-S1    PIC X(1).         
                   20 SACIDASN-D-SELL-ASGN-EXP-DY-X.                      
                      25 SACIDASN-D-SELL-ASGN-EXP-DY PIC 9(2).         
                   20 SACIDASN-D-SELL-ASGN-EXP-S2    PIC X(1).         
                   20 SACIDASN-D-SELL-ASGN-EXP-YR-X.                      
                      25 SACIDASN-D-SELL-ASGN-EXP-YR PIC 9(4).         
       01  SACIDASN-NULL-INDICATORS.                                       
              10 SACIDASN-D-SELL-ASGN-EXP-NI         PIC S9(4)         
                                                     VALUE ZERO
                                                     COMPUTATIONAL.
              10 SACIDASN-NO-NULL-VARIABLES          PIC S9(4)         
                                                     VALUE +0001 
                                                     COMPUTATIONAL.