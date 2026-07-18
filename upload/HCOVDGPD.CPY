      *----------------------------------------------------------------
      *    08/16/21 SJACOB   IR 20-0065 PRICER ENHANCEMENT PHASE 2     
      *                      COPYBOOK FOR TABLE HC_OVRD_PROD_GROUP.    
      *                      INITIAL CREATION                          
      *----------------------------------------------------------------
       01 HC-OVD-GROUP-DETAIL.                                          
          05 HC-GROUP-OVRD-ID          PIC S9(8) VALUE 0 COMPUTATIONAL.
          05 HC-GROUP-OVRD-START-DATE  PIC X(23) VALUE SPACE.          
          05 HC-GROUP-OVRD-EXPIRE-DATE PIC X(23) VALUE SPACE.          
          05 HC-GROUP-OVRD-FLAG        PIC X(01) VALUE SPACE.          
          05 HC-GROUP-OVRD-USER-ID     PIC X(08) VALUE SPACE.
                                                                       
       01  HC-GROUP-NULL-INDICATORS.                                   
              10 HC-GROUP-OVRD-EXP-DATE-NI                              
                                       PIC S9(4) VALUE 0 COMPUTATIONAL.
