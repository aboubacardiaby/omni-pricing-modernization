000010 IDENTIFICATION DIVISION.                                         00000001
000020 PROGRAM-ID.         CUP100.                                      00000002
000030 AUTHOR.             DR HUMPHRIES.                                00000003
000040 DATE-WRITTEN.       960728.                                      00000004
000050****************************************************************  00000005
000060*   ATI COMPILE TYPE = 2    COBOL II / CICS / DB2              *  00000006
000070*                                                              *  00000007
000080*   PROGRAM NAME: CUP100 - SUBROUTINE TO PERFORM ONE-TIME      *  00000008
000090*                          CUSTOMER/ACCOUNT RELATED CALLS      *  00000009
000100*                          TO DB2 TABLES WHEN PRICING ITEMS    *  00000010
000110*                          FOR SAME CUSTOMER                   *  00000011
000120****************************************************************  00000012
000130*                  REVISION HISTORY                            *  00000013
000140*                                                              *  00000014
000150*   DATE      PROGRAMMER              DESCRIPTION              *  00000015
DP0722*--------------------------------------------------------------*
DP0722* 08/2022   DPALEK   WR 22-9470 PRICER ENHANCEMENT             *
DP0722*                        2022 SCOPE                            *
SJ1022*           SJACOB                                             *
BM0421*--------------------------------------------------------------*
BM0421* 04/01/21     BMISHR            WR 20-9002 PRICER ENHANCEMENT *
SJ0421*              SJACOB     ADDED NEW FREIGHT FLAG TABLE AT THE  *
SJ0421*                         CUSTOMER LEVEL.                      *
NK0517*--------------------------------------------------------------*  00000016
NK0517* 05/04/17   CR17-0051 N KIMBO  Changes to supprt DOD GEN V OMN*  00000017
NK0615*--------------------------------------------------------------*  00000016
NK0615* 06/26/15   NDP       N KIMBO    EXPAND OMGPR CR 15-0054 NDP  *  00000017       
HR0415*--------------------------------------------------------------*
HR0415* 04/2015    CR14-0140 HRAI        MOVE THE LUOM CHRG AMOUNT   *
HR0415*                                  AND ACT GRP FEE SWITCH      *
HR0415*                                  FROM CUR120 COPYBOOK TO     *
HR0415*                                  COPYBOOK TO OMGPR COPYBOOK  *
NK0813*--------------------------------------------------------------*
NK0813* 08/06/13   IR13-0361 NKIMBO    MOVE THE NON OM SELECT FEE PCT*
NK0813*                                  FROM CUR120 COPYBOOK TO     *
NK0813*                                  COPYBOOK TO OMGPR COPYBOOK  *
KS0912*--------------------------------------------------------------*
KS0912* 09/13/12   CR11-0093 KEERTHI     MOVE THE LUM & EXTRA DELIV  *
KS0912*                      SHEKAR S    FEES PCT VALUES FROM CUR120 *
KS0912*                                  COPYBOOK TO OMGPR COPYBOOK  *
BM0612*--------------------------------------------------------------*
BM0612* 06/14/12   CR12-0046 BMISHR      EXTRACT BUY GROUP INTERNAL  *
BM0612*                                  NUMBER FROM CUG53 AND PASS  *
BM0612*                                  THEM TO CALLED PROGRAM      *
KS0411*--------------------------------------------------------------*
KS0411* 04/08/11   CR10-0042 KEERTHI     EXTRACT FREIGHT EXEMPTION   *
KS0411*                      SHEKAR S    VALUES FROM CUG53 AND PASS  *
KS0411*                                  THEM TO CALLED PROGRAM      *
NK0408*--------------------------------------------------------------*  00000016
NK0408* 04/05/08   CR07139   N KIMBO     OMGPR EXPANSION -           *  00000017
NK0408*                                  ALSO REMOVE FILE SECTION    *  00000018
NK0408*                                  TO PASS COMPILE             *  00000019
000160* ------------------------------------------------------------ *  00000016
000161* 2004-05-01  SATHYA                                           *  00000017
000162*   CR03-0150 (SA0504)                                         *  00000018
SA0504*   1. POPULATE OMGPR-F-INP-CNT-EXCL-SW WITH 'Y' IF            *  00000019
SA0504*      THERE IS ATLEAST ONE CONTRACT EXCL FOR THE ACT/SHIPTO   *  00000020
000163* ------------------------------------------------------------ *  00000021
000161* 2003-05-01  SATHYA               1. MOVED CUR120-F-LUOM-ELIG-SW 00000022
000162*   INCIDENT: 461097 (SA0403)         TO OMGPR                 *  00000023
000163*                                                              *  00000024
000161* 2002-05-18  N.KIMBO              1. REMOVE LOW VELOCITY      *  00000025
000162*   PROJ# R010104                     CODE.                    *  00000026
000163*                                                              *  00000027
000164* 2001-07-28  G.JONES              1. REMOVE REFERENCES TO     *  00000028
000165*   PROJ# R990002                     CUG16                    *  00000029
000190*                                  2. CALL CUP120 SUBROUTINE   *  00000030
000200*                                     TO RETREIVE ACCOUTN AND  *  00000031
000210*                                     SHIPTO ADJUSTMENTS       *  00000032
000220* 2001-04-07  SATHYA                  MODIFIED TO PERFORM PARA *  00000033
000230*  (INFO TKT:208668)                  A900- ONLY IF THERE HAD  *  00000034
000240*                                     BEEN NO PREVIOUS ERROR   *  00000035
000250* 2001-01-27  SATHYA                  MODIFIED PARA A425- TO   *  00000036
000260*  (REMEDY TKT:1211230)               FOR EFF_DATE <= PRICING  *  00000037
000270*                                     DATE                     *  00000038
000280* 2000-11-13  SATHYA                  INTRODUCED A NEW PARA    *  00000039
000290*                                     A425- TO SELECT INBOUND  *  00000040
000300*                                     FRT FLAG FROM CUG53      *  00000041
000310*                                     INSTEAD OF CUG16         *  00000042
000320*                                  2. ALSO MODED A500- PARA    *  00000043
000330*                                     TO USE SHIP-TO WHILE     *  00000044
000340*                                     SELECTING FROM CUG17     *  00000045
000350* ------------------------------------------------------------ *  00000046
000360* 2000-11-01  SATHYA                  INITED JIT FIELDS IN A900*  00000047
000370*                                     PARA SOTHAT VALUES ALREAD*  00000048
000380*                                     -Y PRESENT WILL NEVER BE *  00000049
000390*                                     BE USED ; ONLY VALUES    *  00000050
000400*                                     READ FROM CUG41 IN THIS  *  00000051
000410*                                     PGM WILL BE USED FURTHER *  00000052
000420* ------------------------------------------------------------ *  00000053
000430* 2000-10-21  SATHYA                  MODIFIED LOGIC TO COMPUTE*  00000054
000440*                                     JIT ADJUSTMENT CHARGES   *  00000055
000450*                                     BY CONSIDERING ZERO PERCE*  00000056
000460*                                     -NT AS A VALID ENTRY IN  *  00000057
000470*                                     CUG41                    *  00000058
000480* -------------------------------------------------------------*  00000059
000490* 2000-08-01  SATHYA                  INCLUDED LOGIC TO COMPUTE*  00000060
000500*                                     JIT ADJUSTMENT CHARGES   *  00000061
000510*                                     FOR AN ACCOUNT (CONTRACT *  00000062
000520*                                     COMPLIANCE PART4 OF PHASE*  00000063
000530*                                     -1)                      *  00000064
000540*                                                              *  00000065
000550* 2000-01-22  SATHYA                  MOVED 'Y' TO             *  00000066
000560*                                     WS-PRIORITY-IND  IN A810-*  00000067
000570*                                     PARA WHEN AC PRIORITY IS *  00000068
000580*                                     FOUND                    *  00000069
000590*                                                              *  00000070
000600* 1999-11-27  GJONES                  BUY GROUP LOW UOM PROJECT*  00000071
000610*                                     POPULATE NEW FIELDS FOR  *  00000072
000620*                                     OMGPR                    *  00000073
000630*                                                              *  00000074
000640* 1999-09-18  GJONES                  BUY GROUP LOW UOM PROJECT*  00000075
000650*                                                              *  00000076
000660*                                                              *  00000077
000670* 1997-10-57  BLYONS                  Y2K PHASE III. VERIFIED  *  00000078
000680*                                     COMPLIANCE.              *  00000079
000690*                                                              *  00000080
000700* 1997-08-07  FRANK LIBERTO           ADDED CALL TO BUY GROUP  *  00000081
000710*                                     TABEL (CUG39) FOR LOW    *  00000082
000720*                                     VELOCITY.  SAME T/O AS   *  00000083
000730*                                     4/24/97 CHANGE. (FL0807) *  00000084
000740*                                                              *  00000085
000750*   LOW VELOCITY (LV) LOGIC IS:                                *  00000086
000760*    1) CHECK ACCOUNT TABLE (CUG36) FOR ACCOUNT EXCEPTION.     *  00000087
000770*       IF FOUND, USE THE ASSOCIATED LV PERCENTAGE, ELSE...    *  00000088
000780*    2) CHECK BUY GRP TABLE (CUG39) FOR ACCOUNT EXCEPTION.     *  00000089
000790*       IF FOUND, USE THE ASSOCIATED LV PERCENTAGE, ELSE...    *  00000090
000800*    3) READ LV DEFAULT TABLE (CUG35)                          *  00000091
000810*       IF FOUND, USE THE ASSOCIATED LV PERCENTAGE, ELSE...    *  00000092
000820*       USE ZERO (+0) FOR PERCENTAGE.  (SHOULD BE FOUND!)      *  00000093
000830*                                                              *  00000094
000840*                                                              *  00000095
000850* 1997-04-24  BOB MATTHEWS (BSI)      ADD LOGIC TO GET LOW     *  00000096
000860*                                     VELOCITY PERCENTAGE.     *  00000097
000870*                                                              *  00000098
000880*                                                              *  00000099
000890****************************************************************  00000100
000900 ENVIRONMENT DIVISION.                                            00000101
000910*                                                                 00000102
000920 DATA DIVISION.                                                   00000103
000950*                                                                 00000106
000960 WORKING-STORAGE SECTION.                                         00000107
000970*                                                                 00000108
000980 77  LCP-WS-ADDR-COMP                 PIC S9(8) COMPUTATIONAL.    00000109
000990 77  LCP-WS-ADDR-PNTR                 REDEFINES LCP-WS-ADDR-COMP  00000110
001000                                            USAGE POINTER.        00000111
001010 01  WS-BEGIN-STORAGE.                                            00000112
001020     10 FILLER                        PIC X(30) VALUE             00000113
001030        'WORKING-STORAGE BEGINS HERE'.                            00000114
001040*                                                                 00000115
001050 01  WS-DATA.
           05 WS-SQLCODE-GOOD          PIC S9(09)  VALUE +000 
                                       COMP-4.
                                                                        00000116
001060     05  WS-ACCOUNT-NOT-FOUND-IND     PIC 9(3)  VALUE  02.        00000117
001070     05  WS-ACT-CUST-NOT-FOUND-IND    PIC 9(3)  VALUE  03.        00000118
001080     05  WS-DIV-NBR-MISSING-IND       PIC 9(3)  VALUE  08.        00000119
001090     05  WS-ACCT-NBR-MISSING-IND      PIC 9(3)  VALUE  09.        00000120
001100     05  WS-DB2-FATAL-ERROR-IND       PIC 9(3)  VALUE  70.        00000121
001130     05  WS-PRIORITY-IND              PIC X(1)  VALUE SPACE.      00000122
001140         88 PRIORITY-FOUND                      VALUE 'Y'.        00000123
001150     05  WS-LOW-UOM-IND               PIC X(1)  VALUE SPACE.      00000124
001160         88 LOW-UOM-FOUND                       VALUE 'Y'.        00000125
001170     05  WS-LOW-UOM-EXCL-IND          PIC X(1)  VALUE SPACE.      00000126
001180         88 LOW-UOM-EXCL-FOUND                  VALUE 'Y'.        00000127
001190     05  WS-BG-PARENT-IND             PIC X(1)  VALUE SPACE.      00000128
001200         88 BG-PARENT-NOT-FOUND                 VALUE 'N'.        00000129
001210     05  WS-DB-OPERATION-MSG.                                     00000130
001220         10  FILLER           PIC X(1)   VALUE     '#'.           00000131
001230         10  WS-DB-ERROR-NBR  PIC 9(3)   VALUE ZEROES.            00000132
001240         10  FILLER           PIC X(1)   VALUE SPACES.            00000133
001250         10  WS-DB-OPERATION  PIC X(8)   VALUE SPACES.            00000134
001260         10  FILLER           PIC X(1)   VALUE SPACES.            00000135
001270         10  WS-DB-NAME       PIC X(18)  VALUE SPACES.            00000136
001280         10  FILLER           PIC X(18)  VALUE                    00000137
001290             ' IN CUP100 CAUSED '.                                00000138
001300         10  WS-DB-STATUS-MSG PIC X(10)  VALUE SPACES.            00000139
001310         10  FILLER           PIC X(08)  VALUE                    00000140
001320             'RTN CODE'.                                          00000141
001330         10 WS-DB-RETURN-CODE PIC -9(6)  VALUE ZEROES.            00000142
NK0615      05  WS-OMAR-CUST-ID.
NK0615         10  WS-OMAR-BR-NBR      PIC X(02).
NK0615         10  WS-OMAR-CUST-NBR    PIC X(06).
SJ1022     05 WS-CUST-ID               PIC X(08) VALUE SPACE.
SJ1022     05 WS-FEE-TYPES.
SJ1022        10 WS-FTYPE-PANDAC-PN    PIC X(02) VALUE 'PN'.
SJ1022        10 WS-FTYPE-SRGTRK-ST    PIC X(02) VALUE 'ST'.
SJ1022        10 WS-FTYPE-BRK-BLK-BB   PIC X(02) VALUE 'BB'.
SJ1022        10 WS-FTYPE-APL-LBL-AL   PIC X(02) VALUE 'AL'.
SJ1022        10 WS-FTYPE-FLUM-SVC-LS  PIC X(02) VALUE 'LS'.
SJ1022     05 WS-PRICE-COMPONENT-TYPES.
SJ1022        10 WS-PCTYPE-FREIGHT-SF  PIC X(02) VALUE 'SF'.
SJ1022        10 WS-PCTYPE-TMSRCHG-TS  PIC X(02) VALUE 'TS'. 
SJ1022        10 WS-PCTYPE-GRP-SAN-GS  PIC X(02) VALUE 'GS'.
SJ1022        10 WS-PCTYPE-GRP-NSC-GN  PIC X(02) VALUE 'GN'.      
SJ1022        10 WS-PCTYPE-IND-MI      PIC X(02) VALUE 'MI'.      
SJ1022        10 WS-PCTYPE-NON-CON-MN  PIC X(02) VALUE 'MN'.      
SJ1022        10 WS-PCTYPE-CUS-MC      PIC X(02) VALUE 'MC'. 
SJ1022     05 WS-CMF-FEE-FOUND-SW      PIC X(01) VALUE SPACES.
SJ1022        88 WS-CMF-FEE-FOUND      VALUE 'Y'.
SJ1022     05 WS-FULL-LUM-SVC-FOUND-SW PIC X(01) VALUE SPACES.
SJ1022        88 WS-FULL-LUM-SVC-FOUND VALUE 'Y'.
001340 01  WS-FOR-JIT-PROCESS.                                          00000143
001350     05 MISC-FOR-JIT.                                             00000144
001360       10  CURR-DATE                    PIC X(10).                00000145
001370     05  WS-JIT-REC.                                              00000146
001380       10  WS-JIT-SERVICE-FEE           PIC X(1).                 00000147
001390       10  WS-JIT-FEE-BREAK-OUT-SW      PIC X(1).                 00000148
001400       10  WS-JIT-SERVICE-FEE-PCT      PIC S9(1)V9(4)             00000149
001410                            VALUE     ZERO                        00000150
001420                            COMPUTATIONAL-3.                      00000151
001430       10  WS-JIT-LABEL-CHRG-TYPE       PIC X(1).                 00000152
001440       10  WS-JIT-LABEL-CHRG-AMT       PIC S9(2)V9(4)             00000153
001450                            VALUE     ZERO                        00000154
001460                            COMPUTATIONAL-3.                      00000155
001470       10  WS-JIT-APPLY-CHRG-TYPE      PIC X(1).                  00000156
001480       10  WS-JIT-APPLY-CHRG-AMT       PIC S9(2)V9(4)             00000157
001490                            VALUE     ZERO                        00000158
001500                            COMPUTATIONAL-3.                      00000159
001510       10  WS-JIT-BREAK-CHRG-TYPE      PIC X(1).                  00000160
001520       10  WS-JIT-BREAK-CHRG-AMT       PIC S9(2)V9(4)             00000161
001530                            VALUE     ZERO                        00000162
001540                            COMPUTATIONAL-3.                      00000163
001550     05  WS-SWITCHES.                                             00000164
001560         10  WS-NOMORE-CUG40-ROW-SW PIC X(01)  VALUE 'N'.         00000165
001570             88  NOMORE-CUG40-ROW           VALUE 'Y'.            00000166
001580         10  WS-NOMORE-CUG41-ROW-SW PIC X(01)  VALUE 'N'.         00000167
001590             88  NOMORE-CUG41-ROW           VALUE 'Y'.            00000168
001600         10  WS-NONSF-JIT-SERVFEE-SW PIC X(01)  VALUE 'N'.        00000169
001610             88  NONSF-JIT-SERVFEE          VALUE 'Y'.            00000170
001620         10  WS-SKIP-CUG41-SW        PIC X(01)  VALUE 'N'.        00000171
001630             88  SKIP-CUG41           VALUE 'Y'.                  00000172
001640     05  WS-CONSTANT.                                             00000173
001650         15 WS-MAX-CELL PIC 9(02)   VALUE 12.                     00000174
001660         15 WS-MAX-ADJTYP PIC 9(02) VALUE 99.                     00000175
001670         15 WS-ADJ-SERVICE-TYPE.                                  00000176
001680             20 WS-ADJSERV-LABEL       PIC X(02) VALUE 'LB'.      00000177
001690             20 WS-ADJSERV-APPLY-LABEL PIC X(02) VALUE 'AL'.      00000178
001700             20 WS-ADJSERV-BREAK-BULK  PIC X(02) VALUE 'BB'.      00000179
001710             20 WS-ADJTYPE-SERVICE-FEE PIC X(02) VALUE 'SF'.      00000180
001720         15 WS-ADJ-TYPES.                                         00000181
001730             20 WS-ADJTYPE-PATIENT-CHG PIC X(02) VALUE 'PL'.      00000182
001740             20 WS-ADJTYPE-APPLY-LABEL PIC X(02) VALUE 'AL'.      00000183
001750             20 WS-ADJTYPE-LABEL-CHG PIC X(02) VALUE 'LB'.        00000184
001760             20 WS-ADJTYPE-BREAK-BULK  PIC X(02) VALUE 'BF'.      00000185
SJ0421*
SJ0421     COPY CUCIDFRT.
SJ0421*
SJ1022     COPY CUVFEEP.
SJ1022     COPY CUVPRCMP.    
SJ1022*
001770     EXEC SQL                                                     00000186
001780       INCLUDE CUG40                                              00000187
001790     END-EXEC.                                                    00000188
001800     EXEC SQL                                                     00000189
001810       INCLUDE CUG41                                              00000190
001820     END-EXEC.                                                    00000191
SA0504*                                                                 00000192
SA0504     EXEC SQL                                                     00000193
SA0404       INCLUDE CCG25                                              00000194
SA0504     END-EXEC.                                                    00000195
SA0504*                                                                 00000196
001830******* CURSOR   DEFINITION**                                     00000197
001840***** CURSOR FOR GETTING CUG41 ROWS FOR A CUSTOMER                00000198
001850         EXEC SQL                                                 00000199
001860           DECLARE CUG41_CSR   CURSOR FOR                         00000200
001870             SELECT CUG41.I_ACCOUNT                               00000201
MFMIGR                   ,CONVERT(CHAR(10),CUG41.D_SERVICE_EFF,101)     00000202
001890                   ,CUG41.I_ACCT_ADJUST_TYPE                      00000203
001900                   ,CUG41.A_SERVICE_FEE                           00000204
001910                   ,CUG41.P_SERVICE_FEE                           00000205
001920                   ,CUG41.C_RATE_TYPE                             00000206
001930                   ,CUG41.C_ACCT_ADJ_LEVEL                        00000207
001940                   ,CUG41.C_APPLY_TO                              00000208
001950                   ,CUG41.F_BREAK_FEE_LINE                        00000209
001960             FROM CUG41                                           00000210
001970              WHERE CUG41.D_SERVICE_EFF                           00000211
001980                           <= :CUG41-D-SERVICE-EFF       AND      00000212
001990                (CUG41.D_SERVICE_EXP >= :CUG41-D-SERVICE-EFF OR   00000213
002000                 CUG41.D_SERVICE_EXP IS NULL)                     00000214
002010                 AND CUG41.I_ACCOUNT = :CUG41-I-ACCOUNT           00000215
002020         END-EXEC.                                                00000216
002030*****************************                                     00000217
002040     EXEC SQL                                                     00000218
002050       INCLUDE BGG03                                              00000219
002060     END-EXEC.                                                    00000220
002070     EXEC SQL                                                     00000221
002080       INCLUDE BGG23                                              00000222
002090     END-EXEC.                                                    00000223
002100     EXEC SQL                                                     00000224
002110       INCLUDE BGG24                                              00000225
002120     END-EXEC.                                                    00000226
002130     EXEC SQL                                                     00000227
002140       INCLUDE CUG02                                              00000228
002150     END-EXEC.                                                    00000229
002160     EXEC SQL                                                     00000230
002170       INCLUDE CUG03                                              00000231
002180     END-EXEC.                                                    00000232
002190     EXEC SQL                                                     00000233
002200       INCLUDE CUG06                                              00000234
002210     END-EXEC.                                                    00000235
002220     EXEC SQL                                                     00000236
002230       INCLUDE CUG07                                              00000237
002240     END-EXEC.                                                    00000238
002250     EXEC SQL                                                     00000239
002260       INCLUDE CUG10                                              00000240
002270     END-EXEC.                                                    00000241
002280     EXEC SQL                                                     00000242
002290       INCLUDE CUG11                                              00000243
002300     END-EXEC.                                                    00000244
002310     EXEC SQL                                                     00000245
002320       INCLUDE CUG53                                              00000246
002330     END-EXEC.                                                    00000247
002370     EXEC SQL                                                     00000248
002380       INCLUDE CUG17                                              00000249
002390     END-EXEC.                                                    00000250
NK0615     EXEC SQL                                                    
NK0615       INCLUDE CUVMST                                             
NK0615     END-EXEC.
002490     EXEC SQL                                                     00000251
002500       INCLUDE SQLCA                                              00000252
002510     END-EXEC.    
002520     EXEC SQL                                                     00000254
002530       INCLUDE OMGPR                                              00000255
002540     END-EXEC.                                                    00000256
002541     EXEC SQL                                                     00000257
002542       INCLUDE CUR120                                             00000258
002543     END-EXEC.                                                    00000259
002544 01  WS-PGM-ID               PIC X(6)   VALUE SPACES.             00000260
002545 01  WS-DUMMY                PIC X(1)   VALUE SPACE.              00000261
002546*                                                                 00000262
002550 01  WS-END-STORAGE.                                              00000263
002560     10 FILLER                PIC X(30)  VALUE                    00000264
002570             'WORKING-STORAGE ENDS HERE'.                         00000265
002580*                                                                 00000266
002590*                                                                 00000267
002600     EJECT                                                        00000268
002610*                                                                 00000269
002620 LINKAGE SECTION.                                                 00000270
002630 01  DFHCOMMAREA.                                                 00000271
NK0615     05  COMMAREA PIC X(1789).                                    00000276
002650*                                                                 00000273
002660 PROCEDURE DIVISION.                                              00000274
002670*                                                                 00000275
002680     MOVE DFHCOMMAREA TO OMGPR.                                   00000276
002690     INITIALIZE OMGPR-PRC-OUTPUT-FIELDS                           00000277
002700                OMGPR-DATA-FROM-CUP100.                           00000278
002710     MOVE SPACES TO OMGPR-ERROR-MESSAGE.                          00000279
002720     PERFORM A100-PRO-PASS-DATA THRU A100-EXIT.                   00000280
002730     IF OMGPR-F-PRICER-ERROR = 'Y'                                00000281
002740       NEXT SENTENCE                                              00000282
002750     ELSE                                                         00000283
002760       PERFORM A200-SEL-ACCOUNT THRU A200-EXIT                    00000284
002770       IF OMGPR-F-PRICER-ERROR = 'Y'                              00000285
002780         NEXT SENTENCE                                            00000286
002790       ELSE                                                       00000287
002800         PERFORM A300-PRO-CUST-INFO THRU A300-EXIT                00000288
002810       END-IF                                                     00000289
002820     END-IF.                                                      00000290
002830                                                                  00000291
SJ1022     IF OMGPR-PRICER-ERROR
SJ1022        CONTINUE 
SJ1022     ELSE 
SJ1022        PERFORM 2000-GET-PRICE-FEE-DATA
SJ1022           THRU 2000-EXIT
SJ1022     END-IF.
SJ1022 
NK0615     IF OMGPR-F-PRICER-ERROR = 'Y'
NK0615        NEXT SENTENCE                                            
NK0615     ELSE                                                       
NK0615        PERFORM A350-PRO-CUTMST-INFO THRU A350-EXIT               
NK0615     END-IF.
002840     IF OMGPR-F-PRICER-ERROR = 'Y'                                00000292
002850       NEXT SENTENCE                                              00000293
002860     ELSE                                                         00000294
002861       PERFORM A400-GET-JIT-ADJ THRU A400-EXIT                    00000295
002880       IF OMGPR-F-PRICER-ERROR = 'Y'                              00000296
002890         CONTINUE                                                 00000297
002900       ELSE                                                       00000298
002910         PERFORM A425-PRO-CUG53                                   00000299
002920            THRU A425-EXIT                                        00000300
002930       END-IF                                                     00000301
002931****** IF OMGPR-F-PRICER-ERROR = 'Y'                              00000302
002932*******  CONTINUE                                                 00000303
002933****** ELSE                                                       00000304
002934*******  PERFORM A500-SEL-SHIP-TO THRU A500-EXIT                  00000305
002935****** END-IF                                                     00000306
002990     END-IF.                                                      00000307
003000                                                                  00000308
003220                                                                  00000325
003230     IF OMGPR-F-PRICER-ERROR = 'Y'                                00000326
003240       CONTINUE                                                   00000327
003250     ELSE                                                         00000328
003260       PERFORM A800-GET-LOW-UOM-PCT THRU A800-EXIT                00000329
003270     END-IF.                                                      00000330
SA0504     IF OMGPR-F-PRICER-ERROR = 'Y'                                00000331
SA0504       CONTINUE                                                   00000332
SA0504     ELSE                                                         00000333
SA0504       PERFORM 1000-VERFIFY-CONTRACT-EXCL                         00000334
               THRU  1000-EXIT                                          00000335
SA0504     END-IF.                                                      00000336
SA0504*                                                                 00000337
SJ1022     IF OMGPR-PRICER-ERROR
SJ1022        CONTINUE 
SJ1022     ELSE 
SJ1022        PERFORM 3000-GET-PRICE-COMPONENT-DATA
SJ1022           THRU 3000-EXIT
SJ1022     END-IF.
SJ1022*
003340     MOVE OMGPR TO DFHCOMMAREA.                                   00000338
003350     EXEC CICS RETURN                                             00000339
003360     END-EXEC.                                                    00000340
003370*                                                                 00000341
003380*                                                                 00000342
003390 A100-PRO-PASS-DATA.                                              00000343
003400*                                                                 00000344
003410     IF OMGPR-I-DIVISION = SPACES                                 00000345
003420     OR LOW-VALUES                                                00000346
003430         MOVE '#102 - MUST PASS DIVISION NUMBER TO CUP100'        00000347
003440                     TO OMGPR-ERROR-MESSAGE                       00000348
003450         MOVE 'Y'    TO OMGPR-F-PRICER-ERROR                      00000349
003460         MOVE 102    TO OMGPR-Q-ERROR-NBR                         00000350
003470         MOVE WS-DIV-NBR-MISSING-IND  TO OMGPR-Q-ERROR-CODE       00000351
003480         GO TO A100-EXIT                                          00000352
003490     END-IF.                                                      00000353
003500*                                                                 00000354
003510     IF OMGPR-S-ACCOUNT = SPACES                                  00000355
003520     OR LOW-VALUES                                                00000356
003530         MOVE '#103 - MUST PASS ACCOUNT NUMBER TO CUP100'         00000357
003540                     TO OMGPR-ERROR-MESSAGE                       00000358
003550         MOVE 'Y'    TO OMGPR-F-PRICER-ERROR                      00000359
003560         MOVE 103    TO OMGPR-Q-ERROR-NBR                         00000360
003570         MOVE WS-ACCT-NBR-MISSING-IND   TO OMGPR-Q-ERROR-CODE     00000361
003580         GO TO A100-EXIT                                          00000362
003590     END-IF.                                                      00000363
003600*                                                                 00000364
003610*                                                                 00000365
003620 A100-EXIT. EXIT.                                                 00000366
003630*                                                                 00000367
003640 A200-SEL-ACCOUNT.                                                00000368
003650     EXEC SQL                                                     00000369
003660       SELECT CUG03.I_CUSTOMER,                                   00000370
003670              CUG03.I_ACCOUNT,                                    00000371
003680              CUG03.C_ACCT_ROUNDING,                              00000372
003690              CUG03.C_ACCT_PRCE_METHOD,                           00000373
BM0421              CUG03.CUSTOMER_NBR
003700        INTO :CUG03-I-CUSTOMER,                                   00000374
003710             :CUG03-I-ACCOUNT,                                    00000375
003720             :CUG03-C-ACCT-ROUNDING,                              00000376
003730             :CUG03-C-ACCT-PRCE-METHOD,                           00000377
BM0421             :CUG03-CUSTOMER-NBR
003740        FROM CUG03                                                00000378
003750        WHERE CUG03.S_ACCOUNT = :OMGPR-S-ACCOUNT                  00000379
003760          AND CUG03.I_DIVISION = :OMGPR-I-DIVISION                00000380
003770     END-EXEC                                                     00000381
003780     EVALUATE SQLCODE                                             00000382
003790         WHEN +0                                                  00000383
003800             CONTINUE                                             00000384
003810         WHEN +100                                                00000385
003820           MOVE '#106 - ACCOUNT NOT FOUND IN CUP100'              00000386
003830              TO OMGPR-ERROR-MESSAGE                              00000387
003840             MOVE 106 TO OMGPR-Q-ERROR-NBR                        00000388
003850             MOVE WS-ACCOUNT-NOT-FOUND-IND TO OMGPR-Q-ERROR-CODE  00000389
003860             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000390
003870         WHEN OTHER                                               00000391
003880             MOVE 'SELECT'    TO WS-DB-OPERATION                  00000392
003890             MOVE 'CUG03'     TO WS-DB-NAME                       00000393
003900             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000394
003910             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000395
003911             MOVE 10          TO WS-DB-ERROR-NBR                  00000396
003921                                 OMGPR-Q-ERROR-NBR                00000397
003940             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000398
003950             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000399
003960             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000400
003970     END-EVALUATE.                                                00000401

003980 A200-EXIT. EXIT.                                                 00000402
003990*                                                                 00000403
004000*                                                                 00000404
004010 A300-PRO-CUST-INFO.                                              00000405
004020     MOVE CUG03-I-CUSTOMER      TO OMGPR-I-CUSTOMER.              00000406
004030     MOVE CUG03-I-ACCOUNT       TO OMGPR-I-ACCOUNT.               00000407
004040     MOVE CUG03-C-ACCT-ROUNDING TO OMGPR-C-ACCT-ROUNDING.         00000408
004050     MOVE CUG03-C-ACCT-PRCE-METHOD TO OMGPR-C-ACCT-PRCE-METHOD.   00000409
BM0421     MOVE CUG03-CUSTOMER-NBR       TO OMGPR-CUSTOMER-NBR. 
004060     PERFORM A310-SEL-ACTIVE-CUST THRU A310-EXIT.                 00000410
004070     MOVE CUG02-C-BUSINESS      TO OMGPR-C-BUSINESS.              00000411
004080 A300-EXIT. EXIT.                                                 00000412
004090*                                                                 00000413
004100 A310-SEL-ACTIVE-CUST.                                            00000414
004110     EXEC SQL                                                     00000415
004120       SELECT CUG02.C_ACTIVE_CUST_STAT,                           00000416
004130              CUG02.C_BUSINESS                                    00000417
004140       INTO :CUG02-C-ACTIVE-CUST-STAT,                            00000418
004150            :CUG02-C-BUSINESS                                     00000419
004160       FROM CUG02                                                 00000420
004170       WHERE CUG02.I_CUSTOMER = :OMGPR-I-CUSTOMER                 00000421
004180     END-EXEC                                                     00000422
004190     EVALUATE SQLCODE                                             00000423
004200         WHEN +0                                                  00000424
004210             CONTINUE                                             00000425
004220         WHEN +100                                                00000426
004230           MOVE '#108 - ACTIVE CUSTOMER NOT FOUND IN '            00000427
004240              TO OMGPR-ERROR-MESSAGE (1:36)                       00000428
004250           MOVE 'CUP100                    ' TO                   00000429
004260             OMGPR-ERROR-MESSAGE (37:25)                          00000430
004270           MOVE 108 TO OMGPR-Q-ERROR-NBR                          00000431
004280           MOVE WS-ACT-CUST-NOT-FOUND-IND  TO OMGPR-Q-ERROR-CODE  00000432
004290           MOVE 'Y' TO OMGPR-F-PRICER-ERROR                       00000433
004300         WHEN OTHER                                               00000434
004310             MOVE 'SELECT'    TO WS-DB-OPERATION                  00000435
004320             MOVE 'CUG02'     TO WS-DB-NAME                       00000436
004330             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000437
004340             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000438
004341             MOVE 20          TO WS-DB-ERROR-NBR                  00000439
004351                                 OMGPR-Q-ERROR-NBR                00000440
004370             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000441
004380             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000442
004390             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000443
004400     END-EVALUATE.                                                00000444

004410 A310-EXIT. EXIT.                                                 00000445
004420*                                                                 00000446
NK0615 A350-PRO-CUTMST-INFO.
NK0615 
NK0615     MOVE 'OM'                   TO CUM-COMPANY-ID.
NK0615     MOVE OMGPR-I-DIVISION       TO WS-OMAR-BR-NBR.
NK0615     MOVE OMGPR-S-ACCOUNT        TO WS-OMAR-CUST-NBR.
NK0615     MOVE WS-OMAR-CUST-ID        TO CUM-CUST-ID.
NK0615
NK0615     EXEC SQL
NK0615           SELECT
NK0615                  STATUS,
NK0615                  CUST_TYPE
NK0615             INTO
NK0615                  :CUM-STATUS,
NK0615                  :CUM-CUST-TYPE
NK0615             FROM CUTMST 
NK0615            WHERE
NK0615                  COMPANY_ID = :CUM-COMPANY-ID AND
NK0615                  CUST_ID    = :CUM-CUST-ID
NK0615     END-EXEC.

             
NK0615     IF SQLCODE =  ZEROES  
NK0615         IF CUM-CUST-TYPE = 'OT'
NK0615            MOVE 'Y' TO OMGPR-CUST-TYPE 
NK0615         ELSE 
NK0615            MOVE 'N' TO OMGPR-CUST-TYPE
NK0615         END-IF 
NK0615     ELSE
NK0615         IF SQLCODE = 100
NK0615             MOVE 'N' TO OMGPR-CUST-TYPE   
NK0615         ELSE                                                     
NK0615             MOVE 'SELECT'    TO WS-DB-OPERATION                  00000491
NK0615             MOVE 'CUTMST'    TO WS-DB-NAME                       00000492
NK0615             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000493
NK0615             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000494
NK0615             MOVE 25          TO WS-DB-ERROR-NBR                  00000495
NK0615                                 OMGPR-Q-ERROR-NBR                00000496
NK0615             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000497
NK0615             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000498
NK0615             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000499
NK0615         END-IF                                                   
NK0615     END-IF.                                                       
NK0615 A350-EXIT. EXIT.
004421 A400-GET-JIT-ADJ.                                                00000447
004422     INITIALIZE   CUR120-RTND-DATA.                               00000448
004423     MOVE OMGPR-I-ACCOUNT TO CUR120-I-ACCOUNT.                    00000449
004424     MOVE OMGPR-D-PRICING TO CUR120-DATE.                         00000450
004425     MOVE OMGPR-C-SHIP-TO-SUFFIX TO CUR120-SHIP-TO.               00000451
NK0517     MOVE OMGPR-CREDIT-ACCT-ORDER TO CUR120-CREDIT-ACCT-ORDER-SW.         
004426     MOVE SPACES          TO CUR120-ACCOUNT-TYPE                  00000452
004427                             CUR120-S-ACCOUNT                     00000453
004428                             CUR120-S-BR.                         00000454
BM0421     MOVE OMGPR-CUSTOMER-NBR     TO CUR120-CUSTOMER-NBR. 
004429     MOVE 'CUP120' TO WS-PGM-ID.                                  00000455
004430     CALL WS-PGM-ID USING WS-DUMMY, WS-DUMMY                      00000456
004431                          SQLCA, CUR120.                          00000457
004432     IF CUR120-GENERAL-RESPONSE-FLAG = 'A' OR                     00000458
004433        CUR120-GENERAL-RESPONSE-FLAG = 'E'                        00000459
004434         MOVE CUR120-GENERAL-RESPONSE-MSG                         00000460
004435            TO OMGPR-ERROR-MESSAGE                                00000461
004436         MOVE 2   TO OMGPR-Q-ERROR-NBR                            00000462
004437         MOVE 'Y' TO OMGPR-F-PRICER-ERROR                         00000463
004438     ELSE                                                         00000464
004439         MOVE CUR120-F-ST-JIT-CUSTOMER TO                         00000465
004440              OMGPR-F-ST-JIT-CUSTOMER                             00000466
004441         MOVE CUR120-JIT-SERVICE-FEE TO                           00000467
004442              OMGPR-JIT-SERVICE-FEE                               00000468
004443         MOVE CUR120-JIT-FEE-BREAK-OUT-SW TO                      00000469
004444              OMGPR-JIT-FEE-BREAK-OUT-SW                          00000470
004445         MOVE CUR120-JIT-SERVICE-FEE-PCT TO                       00000471
004446              OMGPR-JIT-SERVICE-FEE-PCT                           00000472
KS0912         MOVE CUR120-JIT-LUM-FEE-PCT TO
KS0912              OMGPR-JIT-LUM-FEE-PCT
KS0912         MOVE CUR120-JIT-EXTRA-DELIV-FEE-PCT TO
KS0912              OMGPR-JIT-EXTRA-DELIV-FEE-PCT
NK0813         MOVE CUR120-JIT-NON-OM-SLCT-FEE-PCT TO
NK0813              OMGPR-JIT-NON-OM-SLCT-FEE-PCT
004447         MOVE CUR120-JIT-LABEL-CHRG-TYPE TO                       00000473
004448              OMGPR-JIT-LABEL-CHRG-TYPE                           00000474
004449         MOVE CUR120-JIT-LABEL-CHRG-AMT TO                        00000475
004450              OMGPR-JIT-LABEL-CHRG-AMT                            00000476
004451         MOVE CUR120-JIT-APPLY-CHRG-TYPE TO                       00000477
004452              OMGPR-JIT-APPLY-CHRG-TYPE                           00000478
004453         MOVE CUR120-JIT-APPLY-CHRG-AMT TO                        00000479
004454              OMGPR-JIT-APPLY-CHRG-AMT                            00000480
004455         MOVE CUR120-JIT-BREAK-CHRG-TYPE TO                       00000481
004456              OMGPR-JIT-BREAK-CHRG-TYPE                           00000482
004457         MOVE CUR120-JIT-BREAK-CHRG-AMT TO                        00000483
004458              OMGPR-JIT-BREAK-CHRG-AMT                            00000484
HR0415         MOVE CUR120-JIT-LUOM-CHRG-AMT TO
HR0415              OMGPR-JIT-LUOM-CHRG-AMT
HR0415         MOVE CUR120-F-LUOM-ACT-GRP-FEE TO
HR0415              OMGPR-F-LUOM-SW
HR0415         MOVE CUR120-F-BRK-BULK-ACT-GRP-FEE TO
HR0415              OMGPR-F-BREAK-BULK-SW
SA0403         MOVE CUR120-F-LUOM-ELIG-SW     TO OMGPR-F-LUOM-ELIG-SW   00000485
SJ1022         MOVE CUR120-A-BREAKBULK-BB   TO
SJ1022              OMGPR-A-BREAKBULK-BB
SJ1022         MOVE CUR120-P-BREAKBULK-BB   TO 
SJ1022              OMGPR-P-BREAKBULK-BB
SJ1022         MOVE CUR120-FEE-SHRT-CODE-BB TO 
SJ1022              OMGPR-FEE-SHRT-CODE-BB
SJ1022         MOVE CUR120-SKU-CODE-BB      TO 
SJ1022              OMGPR-SKU-CODE-BB
SJ1022         MOVE CUR120-BILLING-FRQ-BB   TO 
SJ1022              OMGPR-BILLING-FRQ-BB
SJ1022         MOVE CUR120-FEE-TYPE-BB      TO 
SJ1022              OMGPR-FEE-TYPE-BB
SJ1022         MOVE CUR120-A-LUM-FEE-AMT-BB TO
SJ1022              OMGPR-A-LUM-FEE-AMT-BB
SJ1022         MOVE CUR120-P-LUM-FEE-PCT-BB TO 
SJ1022              OMGPR-P-LUM-FEE-PCT-BB
SJ1022         MOVE CUR120-A-APPLY-LAB-AL   TO
SJ1022              OMGPR-A-APPLY-LAB-AL
SJ1022         MOVE CUR120-P-APPLY-LAB-AL   TO 
SJ1022              OMGPR-P-APPLY-LAB-AL
SJ1022         MOVE CUR120-FEE-SHRT-CODE-AL TO 
SJ1022              OMGPR-FEE-SHRT-CODE-AL
SJ1022         MOVE CUR120-SKU-CODE-AL      TO 
SJ1022              OMGPR-SKU-CODE-AL
SJ1022         MOVE CUR120-BILLING-FRQ-AL   TO 
SJ1022              OMGPR-BILLING-FRQ-AL
SJ1022         MOVE CUR120-FEE-TYPE-AL      TO
SJ1022              OMGPR-FEE-TYPE-AL
004459     END-IF.                                                      00000486
004860 A400-EXIT. EXIT.                                                 00000487
004870*                                                                 00000488
004880 A425-PRO-CUG53.                                                  00000489
004890     MOVE OMGPR-I-ACCOUNT TO CUG53-I-ACCOUNT                      00000490
004900     EXEC SQL                                                     00000491
KS0411*      SELECT CUG53.F_FRT_FLAG                                    00000492
KS0411*      INTO :CUG53-F-FRT-FLAG                                     00000493
KS0411       SELECT
KS0411          CUG53.F_FRT_FLAG,
KS0411          CUG53.F_EXEMPT_SANC_FLAG,
KS0411          CUG53.F_EXEMPT_NON_SANC_FLAG,
KS0411          CUG53.F_EXEMPT_IND_FLAG,
KS0411          CUG53.F_EXEMPT_NON_CONT_FLAG,
KS0411          CUG53.F_EXEMPT_CUSTOM_FLAG,
BM0612          CUG53.I_BUY_GROUP
KS0411       INTO
KS0411          :CUG53-F-FRT-FLAG,
KS0411          :CUG53-F-GRP-SANC,
KS0411          :CUG53-F-GRP-NON-SANC,
KS0411          :CUG53-F-IND,
KS0411          :CUG53-F-NON-CONT,
KS0411          :CUG53-F-CUSTOM,
BM0612          :CUG53-I-BUY-GROUP
004930       FROM CUG53                                                 00000494
004940       WHERE CUG53.I_ACCOUNT = :CUG53-I-ACCOUNT                   00000495
004950         AND CUG53.D_EFF_DATE <= :OMGPR-D-PRICING                 00000496
004960         AND (D_EXP_DATE >= :OMGPR-D-PRICING                      00000497
004970            OR D_EXP_DATE IS NULL)                                00000498
004980     END-EXEC                                                     00000499
004990     EVALUATE SQLCODE                                             00000500
005000         WHEN +0                                                  00000501
005010             MOVE CUG53-F-FRT-FLAG    TO OMGPR-F-AA-FRT-IN        00000502
KS0411             MOVE CUG53-F-GRP-SANC
KS0411                        TO OMGPR-F-EXEMPT-SANC-FLAG
KS0411             MOVE CUG53-F-GRP-NON-SANC
KS0411                        TO OMGPR-F-EXEMPT-NON-SANC-FLAG
KS0411             MOVE CUG53-F-IND
KS0411                        TO OMGPR-F-EXEMPT-IND-FLAG
KS0411             MOVE CUG53-F-NON-CONT
KS0411                        TO OMGPR-F-EXEMPT-NON-CONT-FLAG
KS0411             MOVE CUG53-F-CUSTOM
KS0411                        TO OMGPR-F-EXEMPT-CUSTOM-FLAG
BM0612             MOVE CUG53-I-BUY-GROUP
BM0612                        TO OMGPR-VAR-I-BUY-GROUP
005020         WHEN +100                                                00000503
SJ0421             PERFORM A450-READ-CID-FREIGHT-FLAG
SJ0421                THRU A450-EXIT
005050         WHEN OTHER                                               00000506
005060             MOVE 'SELECT'    TO WS-DB-OPERATION                  00000507
005070             MOVE 'CUG53'     TO WS-DB-NAME                       00000508
005080             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000509
005090             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000510
005091             MOVE 30          TO WS-DB-ERROR-NBR                  00000511
005101                                 OMGPR-Q-ERROR-NBR                00000512
005120             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000513
005130             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000514
005140             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000515
005150     END-EVALUATE.                                                00000516

005151 A425-EXIT. EXIT.                                                 00000517
005180*                                                                 00000518
SJ0421 A450-READ-CID-FREIGHT-FLAG.
           MOVE OMGPR-CUSTOMER-NBR     TO CUCID-CUSTOMER-NBR.        
           EXEC SQL                                                             
              SELECT
                 F_FRT_FLAG,
                 F_EXEMPT_SANC_FLAG,
                 F_EXEMPT_NON_SANC_FLAG,
                 F_EXEMPT_IND_FLAG,
                 F_EXEMPT_NON_CONT_FLAG,
                 F_EXEMPT_CUSTOM_FLAG,
                 I_BUY_GROUP
              INTO
                 :CUCID-F-FRT-FLAG,
                 :CUCID-F-GRP-SANC,
                 :CUCID-F-GRP-NON-SANC,
                 :CUCID-F-IND,
                 :CUCID-F-NON-CONT,
                 :CUCID-F-CUSTOM,
                 :CUCID-I-BUY-GROUP
              FROM CU_CID_FREIGHT_FLAG                                          
              WHERE CUSTOMER_NBR = :CUCID-CUSTOMER-NBR AND              
                    D_EFF_DATE <= :OMGPR-D-PRICING AND                          
                    (D_EXP_DATE >= :OMGPR-D-PRICING                             
                     OR D_EXP_DATE IS NULL)                                     
           END-EXEC.                                                            
           EVALUATE SQLCODE                                                     
              WHEN +0                                                           
                 MOVE CUCID-F-FRT-FLAG TO OMGPR-F-AA-FRT-IN                     
                 MOVE CUCID-F-GRP-SANC TO OMGPR-F-EXEMPT-SANC-FLAG
                 MOVE CUCID-F-GRP-NON-SANC
                                       TO OMGPR-F-EXEMPT-NON-SANC-FLAG
                 MOVE CUCID-F-IND      TO OMGPR-F-EXEMPT-IND-FLAG
                 MOVE CUCID-F-NON-CONT TO OMGPR-F-EXEMPT-NON-CONT-FLAG
                 MOVE CUCID-F-CUSTOM   TO OMGPR-F-EXEMPT-CUSTOM-FLAG
                 MOVE CUCID-I-BUY-GROUP
                                       TO OMGPR-VAR-I-BUY-GROUP
              WHEN +100
                 MOVE SPACES           TO OMGPR-F-AA-FRT-IN                     
                                          OMGPR-F-EXEMPT-SANC-FLAG
                                          OMGPR-F-EXEMPT-NON-SANC-FLAG
                                          OMGPR-F-EXEMPT-IND-FLAG
                                          OMGPR-F-EXEMPT-NON-CONT-FLAG
                                          OMGPR-F-EXEMPT-CUSTOM-FLAG
                 MOVE ZEROS            TO OMGPR-VAR-I-BUY-GROUP
              WHEN OTHER
                   MOVE 'SELECT'       TO WS-DB-OPERATION                       
                   MOVE 'CU_CID_FREIGHT_FLAG'
                                       TO WS-DB-NAME                            
                   MOVE SQLCODE        TO WS-DB-RETURN-CODE                     
                   MOVE 'DB ERROR'     TO WS-DB-STATUS-MSG                      
                   MOVE 35             TO WS-DB-ERROR-NBR                       
                                          OMGPR-Q-ERROR-NBR                     
                   MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE            
                   MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE           
                   MOVE 'Y'            TO OMGPR-F-PRICER-ERROR                  
           END-EVALUATE.                                                        

SJ0421 A450-EXIT.
           EXIT.

005190 A500-SEL-SHIP-TO.                                                00000519
005200     IF ( OMGPR-C-BILL-TO-SUFFIX = LOW-VALUES ) OR                00000520
005210        ( OMGPR-C-BILL-TO-SUFFIX = SPACES )                       00000521
005220       MOVE ZEROES TO OMGPR-C-BILL-TO-SUFFIX                      00000522
005230                      CUG17-C-SHIP-TO-SUFFIX                      00000523
005240     ELSE                                                         00000524
005250       MOVE OMGPR-C-BILL-TO-SUFFIX  TO                            00000525
005260                      CUG17-C-SHIP-TO-SUFFIX                      00000526
005270     END-IF.                                                      00000527
005280     PERFORM 0510-SELECT-CUG17                                    00000528
005290       THRU  0510-EXIT                                            00000529
005300     IF OMGPR-F-ST-JIT-CUSTOMER = 'Y'                             00000530
005310          GO TO A500-EXIT                                         00000531
005320     END-IF.                                                      00000532
005330     IF OMGPR-C-SHIP-TO-SUFFIX = LOW-VALUES OR SPACES             00000533
005340        CONTINUE                                                  00000534
005350     ELSE                                                         00000535
005360        MOVE OMGPR-C-SHIP-TO-SUFFIX TO                            00000536
005370             CUG17-C-SHIP-TO-SUFFIX                               00000537
005380        PERFORM 0510-SELECT-CUG17                                 00000538
005390          THRU  0510-EXIT                                         00000539
005400          GO TO A500-EXIT                                         00000540
005410     END-IF.                                                      00000541
005420 A500-EXIT. EXIT.                                                 00000542
005430*                                                                 00000543
005440 0510-SELECT-CUG17.                                               00000544
005450     EXEC SQL                                                     00000545
005460       SELECT CUG17.F_ST_JIT_CUSTOMER,                            00000546
005470              CUG17.C_ST_JIT_LABELS,                              00000547
005480              CUG17.C_ST_JIT_LABEL_FMT,                           00000548
005490              CUG17.C_ST_JIT_APPLY_LBL,                           00000549
005500              CUG17.C_ST_JIT_BREAK_BLK,                           00000550
005510              CUG17.F_ST_JIT_PART_SUB,                            00000551
005520              CUG17.F_ST_PANDAC                                   00000552
005530       INTO :CUG17-F-ST-JIT-CUSTOMER,                             00000553
005540            :CUG17-C-ST-JIT-LABELS,                               00000554
005550            :CUG17-C-ST-JIT-LABEL-FMT,                            00000555
005560            :CUG17-C-ST-JIT-APPLY-LBL,                            00000556
005570            :CUG17-C-ST-JIT-BREAK-BLK,                            00000557
005580            :CUG17-F-ST-JIT-PART-SUB,                             00000558
005590            :CUG17-F-ST-PANDAC                                    00000559
005600       FROM CUG17                                                 00000560
005610       WHERE CUG17.I_ACCOUNT = :OMGPR-I-ACCOUNT                   00000561
005620         AND CUG17.C_SHIP_TO_SUFFIX = :CUG17-C-SHIP-TO-SUFFIX     00000562
005630     END-EXEC                                                     00000563
005640     EVALUATE SQLCODE                                             00000564
005650         WHEN +0                                                  00000565
005660             IF CUG17-F-ST-JIT-CUSTOMER = 'Y'                     00000566
005670               MOVE CUG17-F-ST-JIT-CUSTOMER TO                    00000567
005680                    OMGPR-F-ST-JIT-CUSTOMER                       00000568
005690             ELSE                                                 00000569
005700               INITIALIZE OMGPR-JIT-PRICING-FIELDS                00000570
005710             END-IF                                               00000571
005720         WHEN +100                                                00000572
005730             MOVE '#120 - SHIP-TO SUFFIX NOT FOUND'               00000573
005740                TO OMGPR-ERROR-MESSAGE (1:36)                     00000574
005750             MOVE 120 TO OMGPR-Q-ERROR-NBR                        00000575
005760             MOVE 'CUP100                    '                    00000576
005770                TO OMGPR-ERROR-MESSAGE (37:25)                    00000577
005780             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000578
005790         WHEN OTHER                                               00000579
005800             MOVE 'SELECT'    TO WS-DB-OPERATION                  00000580
005810             MOVE 'CUG17'     TO WS-DB-NAME                       00000581
005820             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000582
005830             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000583
005831             MOVE 40          TO WS-DB-ERROR-NBR                  00000584
005841                                 OMGPR-Q-ERROR-NBR                00000585
005860             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000586
005870             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000587
005880             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000588
005890     END-EVALUATE.                                                00000589

005900 0510-EXIT. EXIT.                                                 00000590
005910*                                                                 00000600
006840                                                                  00000677
006850 A800-GET-LOW-UOM-PCT.                                            00000678
HR0415*    MOVE 'N'    TO OMGPR-F-LOW-UOM.                              00000679
006861     MOVE +0     TO OMGPR-P-LOW-UOM                               00000680
006862                    OMGPR-I-BUY-GROUP-LUOM.                       00000681
HR0415*    MOVE SPACES TO OMGPR-C-LUOM-EXCL                             00000682
HR0415     MOVE SPACES TO OMGPR-F-LUOM-VEND-EXCL
006865                    OMGPR-D-BG-LOW-UOM-EFF.                       00000684
006920                                                                  00000685
006930     PERFORM A810-SEL-ACCT-PRIORITY THRU A810-EXIT.               00000686
006940     IF PRIORITY-FOUND                                            00000687
006950         PERFORM A830-SEL-LOW-UOM THRU A830-EXIT                  00000688
006960         IF LOW-UOM-FOUND                                         00000689
006970             CONTINUE                                             00000690
006980         ELSE                                                     00000691
006990             MOVE BGG23-I-BUY-GROUP TO BGG03-I-BUY-GROUP          00000692
007000             PERFORM A850-SEL-PARENTS THRU A850-EXIT              00000693
007010             IF LOW-UOM-FOUND OR BG-PARENT-NOT-FOUND              00000694
007020                 CONTINUE                                         00000695
007030             ELSE                                                 00000696
007040                 MOVE BGG23-I-BUY-GROUP TO BGG03-I-BUY-GROUP      00000697
007050                 PERFORM A850-SEL-PARENTS THRU A850-EXIT          00000698
007060             END-IF                                               00000699
007070         END-IF                                                   00000700
007080     END-IF.                                                      00000701
007090 A800-EXIT. EXIT.                                                 00000702
007100                                                                  00000703
007110 A810-SEL-ACCT-PRIORITY.                                          00000704
007120     EXEC SQL                                                     00000705
007130         SELECT CUG11.I_BUY_GROUP                                 00000706
007140         INTO  :CUG11-I-BUY-GROUP                                 00000707
007150         FROM CUG10, CUG11                                        00000708
007160         WHERE CUG10.I_ACCOUNT = :OMGPR-I-ACCOUNT                 00000709
007170         AND CUG10.D_ACCT_BG_PRI_EFF <= :OMGPR-D-PRICING          00000710
007180         AND (CUG10.D_ACCT_BG_PRI_EXP >= :OMGPR-D-PRICING OR      00000711
007190              CUG10.D_ACCT_BG_PRI_EXP IS NULL)                    00000712
007200         AND CUG11.I_ACCOUNT = CUG10.I_ACCOUNT                    00000713
007210         AND CUG11.D_ACCT_BG_PRI_EFF = CUG10.D_ACCT_BG_PRI_EFF    00000714
007220         AND CUG11.Q_ACCT_BG_PRIORITY = 1                         00000715
007230     END-EXEC                                                     00000716
007240     EVALUATE SQLCODE                                             00000717
007250         WHEN 0                                                   00000718
007260             MOVE 'Y' TO WS-PRIORITY-IND                          00000719
007270             MOVE CUG11-I-BUY-GROUP TO BGG23-I-BUY-GROUP          00000720
007280         WHEN 100                                                 00000721
007290             PERFORM A820-SEL-CUST-PRIORITY THRU A820-EXIT        00000722
007300         WHEN OTHER                                               00000723
007310             MOVE 'SELECT '   TO WS-DB-OPERATION                  00000724
007320             MOVE 'CUG10,CUG11'  TO WS-DB-NAME                    00000725
007330             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000726
007340             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000727
007341             MOVE 50          TO WS-DB-ERROR-NBR                  00000728
007351                                 OMGPR-Q-ERROR-NBR                00000729
007370             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000730
007380             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000731
007390             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000732
007400         END-EVALUATE.                                            00000733

007410 A810-EXIT. EXIT.                                                 00000734
007420                                                                  00000735
007430 A820-SEL-CUST-PRIORITY.                                          00000736
007440     EXEC SQL                                                     00000737
007450         SELECT CUG07.I_BUY_GROUP                                 00000738
007460         INTO  :CUG07-I-BUY-GROUP                                 00000739
007470         FROM CUG06, CUG07                                        00000740
007480         WHERE CUG06.I_CUSTOMER = :OMGPR-I-CUSTOMER               00000741
007490         AND CUG06.D_CUST_BG_PRI_EFF <= :OMGPR-D-PRICING          00000742
007500         AND (CUG06.D_CUST_BG_PRI_EXP >= :OMGPR-D-PRICING OR      00000743
007510              CUG06.D_CUST_BG_PRI_EXP IS NULL)                    00000744
007520         AND CUG07.I_CUSTOMER = CUG06.I_CUSTOMER                  00000745
007530         AND CUG07.D_CUST_BG_PRI_EFF = CUG06.D_CUST_BG_PRI_EFF    00000746
007540         AND CUG07.Q_CUST_BG_PRIORITY = 1                         00000747
007550     END-EXEC                                                     00000748
007560     EVALUATE SQLCODE                                             00000749
007570         WHEN 0                                                   00000750
007580             MOVE 'Y' TO WS-PRIORITY-IND                          00000751
007590             MOVE CUG07-I-BUY-GROUP TO BGG23-I-BUY-GROUP          00000752
007600         WHEN 100                                                 00000753
007610             CONTINUE                                             00000754
007620         WHEN OTHER                                               00000755
007630             MOVE 'SELECT '   TO WS-DB-OPERATION                  00000756
007640             MOVE 'CUG06,CUG07'  TO WS-DB-NAME                    00000757
007650             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000758
007660             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000759
007661             MOVE 60          TO WS-DB-ERROR-NBR                  00000760
007671                                 OMGPR-Q-ERROR-NBR                00000761
007690             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000762
007700             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000763
007710             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000764
007720       END-EVALUATE.                                              00000765

007730 A820-EXIT. EXIT.                                                 00000766
007740                                                                  00000767
007750 A830-SEL-LOW-UOM.                                                00000768
007760     EXEC SQL                                                     00000769
007770       SELECT BGG23.P_BG_LOW_UOM,                                 00000770
MFMIGR              CONVERT(CHAR(10),BGG23.D_BG_LOW_UOM_EFF,101),       00000771
HR0415*             BGG23.C_LUOM_EXCL,                                  00000772
HR0415              BGG23.P_BG_BREAK_BULK,
007800              BGG23.F_LUOM_VEND_EXCL                              00000773
007810       INTO  :BGG23-P-BG-LOW-UOM,                                 00000774
007820             :BGG23-D-BG-LOW-UOM-EFF,                             00000775
HR0415*            :BGG23-C-LUOM-EXCL,
HR0415             :BGG23-P-BG-BRK-BULK,
007840             :BGG23-F-LUOM-VEND-EXCL                              00000777
007850       FROM BGG23                                                 00000778
007860       WHERE BGG23.I_BUY_GROUP = :BGG23-I-BUY-GROUP               00000779
007870       AND   BGG23.D_BG_LOW_UOM_EFF <= :OMGPR-D-PRICING           00000780
007880       AND  (BGG23.D_BG_LOW_UOM_EXP >= :OMGPR-D-PRICING           00000781
007890       OR    BGG23.D_BG_LOW_UOM_EXP IS NULL)                      00000782
007900       END-EXEC                                                   00000783
007910       EVALUATE SQLCODE                                           00000784
007920           WHEN 0                                                 00000785
007930               MOVE 'Y' TO WS-LOW-UOM-IND                         00000786
007940               MOVE BGG23-I-BUY-GROUP TO BGG24-I-BUY-GROUP        00000787
007950               PERFORM A840-SEL-LOW-UOM-EXCL THRU A840-EXIT       00000788
007960            WHEN 100                                              00000789
007970               CONTINUE                                           00000790
007980            WHEN OTHER                                            00000791
007990             MOVE 'SELECT '   TO WS-DB-OPERATION                  00000792
008000             MOVE 'BGG23'     TO WS-DB-NAME                       00000793
008010             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000794
008020             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000795
008021             MOVE 70          TO WS-DB-ERROR-NBR                  00000796
008031                                 OMGPR-Q-ERROR-NBR                00000797
008050             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000798
008060             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000799
008070             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000800
008080        END-EVALUATE.                                             00000801

008090 A830-EXIT. EXIT.                                                 00000802
008100                                                                  00000803
008110 A840-SEL-LOW-UOM-EXCL.                                           00000804
008120     EXEC SQL                                                     00000805
008130       SELECT BGG24.I_BUY_GROUP                                   00000806
008140       INTO  :BGG24-I-BUY-GROUP                                   00000807
008150       FROM BGG24                                                 00000808
008160       WHERE BGG24.I_BUY_GROUP = :BGG23-I-BUY-GROUP               00000809
008170       AND   BGG24.D_BG_LOW_UOM_EFF = :BGG23-D-BG-LOW-UOM-EFF     00000810
008180       AND   BGG24.I_CUSTOMER = :OMGPR-I-CUSTOMER                 00000811
008190       AND   BGG24.D_BG_LUOM_EXCL_EFF <= :OMGPR-D-PRICING         00000812
008200       AND  (BGG24.D_BG_LUOM_EXCL_EXP >= :OMGPR-D-PRICING         00000813
008210       OR    BGG24.D_BG_LUOM_EXCL_EXP IS NULL)                    00000814
008220       END-EXEC                                                   00000815
008230       EVALUATE SQLCODE                                           00000816
008240           WHEN 0                                                 00000817
008250               CONTINUE                                           00000818
008260            WHEN 100                                              00000819
008270                MOVE BGG23-I-BUY-GROUP  TO OMGPR-I-BUY-GROUP-LUOM 00000820
008280                MOVE BGG23-D-BG-LOW-UOM-EFF TO                    00000821
008290                                     OMGPR-D-BG-LOW-UOM-EFF       00000822
008300                MOVE BGG23-P-BG-LOW-UOM TO OMGPR-P-LOW-UOM        00000823
HR0415*               MOVE BGG23-C-LUOM-EXCL  TO OMGPR-C-LUOM-EXCL      00000892
HR0415                MOVE BGG23-P-BG-BRK-BULK     TO
HR0415                                       OMGPR-P-BREAK-BULK
008320                MOVE BGG23-F-LUOM-VEND-EXCL  TO                   00000825
008330                                      OMGPR-F-LUOM-VEND-EXCL      00000826
008340            WHEN OTHER                                            00000827
008350             MOVE 'SELECT '   TO WS-DB-OPERATION                  00000828
008360             MOVE 'BGG24'     TO WS-DB-NAME                       00000829
008370             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000830
008380             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000831
008390             MOVE 80          TO WS-DB-ERROR-NBR                  00000832
008391                                 OMGPR-Q-ERROR-NBR                00000833
008410             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000834
008420             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000835
008430             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000836
008440        END-EVALUATE.                                             00000837

008450 A840-EXIT. EXIT.                                                 00000838
008460                                                                  00000839
008470 A850-SEL-PARENTS.                                                00000840
008480       EXEC SQL                                                   00000841
008490       SELECT BGG03.I_BUY_GROUP_PARENT                            00000842
008500       INTO :BGG03-I-BUY-GROUP-PARENT                             00000843
008510       FROM BGG03                                                 00000844
008520       WHERE BGG03.I_BUY_GROUP = :BGG03-I-BUY-GROUP               00000845
008530       AND BGG03.D_PARTNER_START <= :OMGPR-D-PRICING              00000846
008540       AND (BGG03.D_PARTNER_EXPIRE >= :OMGPR-D-PRICING            00000847
008550       OR BGG03.D_PARTNER_EXPIRE IS NULL)                         00000848
008560       END-EXEC                                                   00000849
008570       EVALUATE SQLCODE                                           00000850
008580           WHEN 0                                                 00000851
008590               MOVE 'Y' TO WS-BG-PARENT-IND                       00000852
008600               MOVE BGG03-I-BUY-GROUP-PARENT TO                   00000853
008610                    BGG23-I-BUY-GROUP                             00000854
008620               PERFORM A830-SEL-LOW-UOM THRU A830-EXIT            00000855
008630            WHEN 100                                              00000856
008640                MOVE 'N' TO WS-BG-PARENT-IND                      00000857
008650            WHEN OTHER                                            00000858
008660             MOVE 'SELECT '   TO WS-DB-OPERATION                  00000859
008670             MOVE 'BGG03'     TO WS-DB-NAME                       00000860
008680             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000861
008690             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000862
008691             MOVE 90          TO WS-DB-ERROR-NBR                  00000863
008701                                 OMGPR-Q-ERROR-NBR                00000864
008720             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000865
008730             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000866
008740             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000867
008750        END-EVALUATE.                                             00000868

008760 A850-EXIT. EXIT.                                                 00000869
013650*                                                                 00000870
SA0504 1000-VERFIFY-CONTRACT-EXCL.                                      00000880
SA0504      MOVE 'N'                           TO                       00000881
SA0504                                          OMGPR-F-INP-CNT-EXCL-SW.00000882
SA0504      MOVE OMGPR-I-ACCOUNT               TO CCG25-I-ACCOUNT.      00000883
SA0504      EXEC SQL                                                    00000884
008490         SELECT 'Y'                                               00000885
SA0504           INTO :OMGPR-F-INP-CNT-EXCL-SW                          00000886
SA0504           FROM CCG25                                             00000887
SA0504             WHERE CCG25.I_ACCOUNT   = :CCG25-I-ACCOUNT           00000888
SA0504      END-EXEC                                                    00000892
SA0504      EVALUATE SQLCODE                                            00000893
SA0504           WHEN 0                                                 00000894
SA0504               CONTINUE                                           00000895
SA0504           WHEN 100                                               00000896
SA0504               MOVE 'N'  TO  OMGPR-F-INP-CNT-EXCL-SW              00000897
SA0504           WHEN -811                                              00000898
SA0504               MOVE 'Y'  TO  OMGPR-F-INP-CNT-EXCL-SW              00000899
SA0504            WHEN OTHER                                            00000901
SA0504             MOVE 'SELECT '   TO WS-DB-OPERATION                  00000902
SA0504             MOVE 'CCG25'     TO WS-DB-NAME                       00000903
SA0504             MOVE SQLCODE     TO WS-DB-RETURN-CODE                00000904
SA0504             MOVE 'DB ERROR'  TO WS-DB-STATUS-MSG                 00000905
SA0504             MOVE 1000        TO WS-DB-ERROR-NBR                  00000906
SA0504                                 OMGPR-Q-ERROR-NBR                00000907
SA0504             MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE    00000908
SA0504             MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE   00000909
SA0504             MOVE 'Y' TO OMGPR-F-PRICER-ERROR                     00000910
SA0504      END-EVALUATE.                                               00000911

SA0504 1000-EXIT.                                                       00000912
SA0504      EXIT.                                                       00000920
SA0504*                                                                 00001000
SJ1022 2000-GET-PRICE-FEE-DATA.
           PERFORM 2010-INIT-FEE-FIELDS THRU 2010-EXIT.
           MOVE 'OM'                   TO CUFP-COMPANY-ID.
           MOVE OMGPR-S-ACCOUNT        TO WS-CUST-ID(3:6).
           MOVE OMGPR-I-DIVISION       TO WS-CUST-ID(1:2).
           MOVE '0'                    TO CUFP-SHIP-TO-SUFFIX(1:1).
           MOVE OMGPR-C-SHIP-TO-SUFFIX TO CUFP-SHIP-TO-SUFFIX(2:3).
           MOVE SPACES                 TO WS-CMF-FEE-FOUND-SW.

           PERFORM 2100-GET-PANDAC-FEE THROUGH 2100-EXIT.
           IF OMGPR-PRICER-ERROR   
              CONTINUE 
           ELSE 
              PERFORM 2200-GET-SURGITRAK-FEE THROUGH 2200-EXIT 
           END-IF. 

SJ1022 2000-EXIT.
           EXIT.

SJ1022 2010-INIT-FEE-FIELDS.

           MOVE 0               TO OMGPR-A-SURGITRAK-ST     
                                   OMGPR-P-SURGITRAK-ST     
                                   OMGPR-A-BREAKBULK-BB     
                                   OMGPR-P-BREAKBULK-BB     
                                   OMGPR-A-LUM-FEE-AMT-BB  
                                   OMGPR-P-LUM-FEE-PCT-BB  
                                   OMGPR-A-APPLY-LAB-AL    
                                   OMGPR-P-APPLY-LAB-AL.     
                                                       
           MOVE SPACES          TO OMGPR-FEE-SHRT-CODE-ST 
                                   OMGPR-SKU-CODE-ST 
                                   OMGPR-BILLING-FRQ-ST 
                                   OMGPR-FEE-TYPE-ST   
                                   OMGPR-FEE-SHRT-CODE-BB 
                                   OMGPR-SKU-CODE-BB   
                                   OMGPR-BILLING-FRQ-BB 
                                   OMGPR-FEE-TYPE-BB     
                                   OMGPR-FEE-SHRT-CODE-AL 
                                   OMGPR-SKU-CODE-AL      
                                   OMGPR-BILLING-FRQ-AL   
                                   OMGPR-FEE-TYPE-AL     
                                   OMGPR-FEE-SHRT-CODE-SF 
                                   OMGPR-SKU-CODE-SF      
                                   OMGPR-BILLING-FRQ-SF   
                                   OMGPR-FEE-SHRT-CODE-TS 
                                   OMGPR-SKU-CODE-TS       
                                   OMGPR-BILLING-FRQ-TS   
                                   OMGPR-FEE-SHRT-CODE-GS  
                                   OMGPR-SKU-CODE-GS       
                                   OMGPR-BILLING-FRQ-GS   
                                   OMGPR-FEE-SHRT-CODE-GN  
                                   OMGPR-SKU-CODE-GN      
                                   OMGPR-BILLING-FRQ-GN    
                                   OMGPR-FEE-SHRT-CODE-MI  
                                   OMGPR-SKU-CODE-MI       
                                   OMGPR-BILLING-FRQ-MI    
                                   OMGPR-FEE-SHRT-CODE-MN  
                                   OMGPR-SKU-CODE-MN       
                                   OMGPR-BILLING-FRQ-MN    
                                   OMGPR-FEE-SHRT-CODE-MC  
                                   OMGPR-SKU-CODE-MC       
                                   OMGPR-BILLING-FRQ-MC.  

SJ1022 2010-EXIT. 
           EXIT. 

SJ1022 2100-GET-PANDAC-FEE.
           MOVE WS-FTYPE-PANDAC-PN     TO CUFP-FEE-SHORT-CODE.
           PERFORM 4000-SQL-SELECT-CUTFEE-PRICE THROUGH 4000-EXIT.

           EVALUATE SQLCODE
              WHEN 0
                 MOVE CUFP-FEE-SHORT-CODE 
                                       TO OMGPR-FEE-SHRT-CODE-PN
                 MOVE CUFP-FEE-TYPE    TO OMGPR-FEE-TYPE-PN
                 COMPUTE OMGPR-P-PANDAC-PN = CUFP-FEE-PCT / 100
                 MOVE CUFP-FEE-AMT     TO OMGPR-A-PANDAC-PN
                 MOVE CUFP-SKU-CODE    TO OMGPR-SKU-CODE-PN
                 MOVE CUFP-BILLING-FREQ
                                       TO OMGPR-BILLING-FRQ-PN
                 MOVE 'Y'              TO WS-CMF-FEE-FOUND-SW
              WHEN 100
                 MOVE SPACES           TO OMGPR-FEE-SHRT-CODE-PN
                                          OMGPR-FEE-TYPE-PN
                                          OMGPR-SKU-CODE-PN
                                          OMGPR-BILLING-FRQ-PN
                 MOVE ZEROS            TO OMGPR-P-PANDAC-PN
                                          OMGPR-A-PANDAC-PN
              WHEN OTHER
                 MOVE 'SELECT'         TO WS-DB-OPERATION                       
                 MOVE 'CUTFEE_PRICE'   TO WS-DB-NAME                    
                 MOVE SQLCODE          TO WS-DB-RETURN-CODE                     
                 MOVE 'DB ERROR'       TO WS-DB-STATUS-MSG                      
                 MOVE 220              TO WS-DB-ERROR-NBR                       
                                          OMGPR-Q-ERROR-NBR                     
                 MOVE WS-DB2-FATAL-ERROR-IND
                                       TO OMGPR-Q-ERROR-CODE                     
                 MOVE WS-DB-OPERATION-MSG
                                       TO OMGPR-ERROR-MESSAGE                   
                 MOVE 'Y'              TO OMGPR-F-PRICER-ERROR                  
           END-EVALUATE.                                                

SJ1022 2100-EXIT.
           EXIT. 
SJ1022 2200-GET-SURGITRAK-FEE.
           MOVE WS-FTYPE-SRGTRK-ST     TO CUFP-FEE-SHORT-CODE.
           PERFORM 4000-SQL-SELECT-CUTFEE-PRICE THROUGH 4000-EXIT.

           EVALUATE SQLCODE
              WHEN 0
                 MOVE CUFP-FEE-SHORT-CODE 
                                       TO OMGPR-FEE-SHRT-CODE-ST
                 MOVE CUFP-FEE-TYPE    TO OMGPR-FEE-TYPE-ST
                 COMPUTE OMGPR-P-SURGITRAK-ST = CUFP-FEE-PCT / 100
                 MOVE CUFP-FEE-AMT     TO OMGPR-A-SURGITRAK-ST
                 MOVE CUFP-SKU-CODE    TO OMGPR-SKU-CODE-ST
                 MOVE CUFP-BILLING-FREQ
                                       TO OMGPR-BILLING-FRQ-ST
                 MOVE 'Y'              TO WS-CMF-FEE-FOUND-SW
              WHEN 100
                 MOVE SPACES           TO OMGPR-FEE-SHRT-CODE-ST
                                          OMGPR-FEE-TYPE-ST
                                          OMGPR-SKU-CODE-ST
                                          OMGPR-BILLING-FRQ-ST
                 MOVE ZEROS            TO OMGPR-P-SURGITRAK-ST
                                          OMGPR-A-SURGITRAK-ST
              WHEN OTHER
                 MOVE 'SELECT'         TO WS-DB-OPERATION                       
                 MOVE 'CUTFEE_PRICE'   TO WS-DB-NAME                    
                 MOVE SQLCODE          TO WS-DB-RETURN-CODE                     
                 MOVE 'DB ERROR'       TO WS-DB-STATUS-MSG                      
                 MOVE 220              TO WS-DB-ERROR-NBR                       
                                          OMGPR-Q-ERROR-NBR                     
                 MOVE WS-DB2-FATAL-ERROR-IND
                                       TO OMGPR-Q-ERROR-CODE                     
                 MOVE WS-DB-OPERATION-MSG
                                       TO OMGPR-ERROR-MESSAGE                   
                 MOVE 'Y'              TO OMGPR-F-PRICER-ERROR                  
           END-EVALUATE.                                                


SJ1022 2200-EXIT.
           EXIT.

SJ1022 3000-GET-PRICE-COMPONENT-DATA.
           MOVE 'OM'                   TO CUPC-COMPANY-ID.
           MOVE OMGPR-S-ACCOUNT        TO WS-CUST-ID(3:6).
           MOVE OMGPR-I-DIVISION       TO WS-CUST-ID(1:2).
           MOVE '0'                    TO CUPC-SHIP-TO-SUFFIX(1:1).
           MOVE OMGPR-C-SHIP-TO-SUFFIX TO CUPC-SHIP-TO-SUFFIX(2:3).

           PERFORM 3100-GET-FREIGHT-DATA THROUGH 3100-EXIT.

           IF OMGPR-PRICER-ERROR
              CONTINUE 
           ELSE 
              PERFORM 3200-GET-TMSRCHG-DATA THROUGH 3200-EXIT 
           END-IF. 

           IF OMGPR-PRICER-ERROR
              CONTINUE 
           ELSE 
              PERFORM 3300-GET-GRP-SAN-DATA THROUGH 3300-EXIT 
           END-IF. 

           IF OMGPR-PRICER-ERROR
              CONTINUE 
           ELSE 
              PERFORM 3400-GET-GRP-NSC-DATA THROUGH 3400-EXIT
           END-IF. 

           IF OMGPR-PRICER-ERROR
              CONTINUE 
           ELSE 
              PERFORM 3500-GET-IND-DATA THROUGH 3500-EXIT
           END-IF. 

           IF OMGPR-PRICER-ERROR
              CONTINUE 
           ELSE 
              PERFORM 3600-GET-NON-CON-DATA THROUGH 3600-EXIT
           END-IF. 

           IF OMGPR-PRICER-ERROR
              CONTINUE 
           ELSE 
              PERFORM 3700-GET-CUS-DATA THROUGH 3700-EXIT
           END-IF. 


SJ1022 3000-EXIT.
           EXIT.

SJ1022 3100-GET-FREIGHT-DATA.
           MOVE WS-PCTYPE-FREIGHT-SF   TO CUPC-COMP-SHORT-CODE.
           PERFORM 5000-SQL-SELECT-PRICE-COMP THROUGH 5000-EXIT.

           EVALUATE SQLCODE
              WHEN 0
                 MOVE CUPC-COMP-SHORT-CODE 
                                       TO OMGPR-FEE-SHRT-CODE-SF 
                 MOVE CUPC-SKU-CODE    TO OMGPR-SKU-CODE-SF
                 MOVE CUPC-BILLING-FREQ
                                       TO OMGPR-BILLING-FRQ-SF
              WHEN 100
                 MOVE SPACES           TO OMGPR-FEE-SHRT-CODE-SF 
                                          OMGPR-SKU-CODE-SF
                                          OMGPR-BILLING-FRQ-SF
              WHEN OTHER
                 MOVE 'SELECT'         TO WS-DB-OPERATION                       
                 MOVE 'CUTPRICE_COMPONENT'
                                       TO WS-DB-NAME
                 MOVE SQLCODE          TO WS-DB-RETURN-CODE                     
                 MOVE 'DB ERROR'       TO WS-DB-STATUS-MSG                      
                 MOVE 310              TO WS-DB-ERROR-NBR                      
                                          OMGPR-Q-ERROR-NBR                     
                 MOVE WS-DB2-FATAL-ERROR-IND
                                       TO OMGPR-Q-ERROR-CODE                     
                 MOVE WS-DB-OPERATION-MSG
                                       TO OMGPR-ERROR-MESSAGE                   
                 MOVE 'Y'              TO OMGPR-F-PRICER-ERROR                  
           END-EVALUATE.                                                


SJ1022 3100-EXIT.
           EXIT.

SJ1022 3200-GET-TMSRCHG-DATA.
           MOVE WS-PCTYPE-TMSRCHG-TS   TO CUPC-COMP-SHORT-CODE.
           PERFORM 5000-SQL-SELECT-PRICE-COMP THROUGH 5000-EXIT.

           EVALUATE SQLCODE
              WHEN 0
                 MOVE CUPC-COMP-SHORT-CODE 
                                       TO OMGPR-FEE-SHRT-CODE-TS 
                 MOVE CUPC-SKU-CODE    TO OMGPR-SKU-CODE-TS
                 MOVE CUPC-BILLING-FREQ
                                       TO OMGPR-BILLING-FRQ-TS
              WHEN 100
                 MOVE SPACES           TO OMGPR-FEE-SHRT-CODE-TS
                                          OMGPR-SKU-CODE-TS
                                          OMGPR-BILLING-FRQ-TS
              WHEN OTHER
                 MOVE 'SELECT'         TO WS-DB-OPERATION                       
                 MOVE 'CUTPRICE_COMPONENT'
                                       TO WS-DB-NAME
                 MOVE SQLCODE          TO WS-DB-RETURN-CODE                     
                 MOVE 'DB ERROR'       TO WS-DB-STATUS-MSG                      
                 MOVE 320              TO WS-DB-ERROR-NBR                      
                                          OMGPR-Q-ERROR-NBR                     
                 MOVE WS-DB2-FATAL-ERROR-IND
                                       TO OMGPR-Q-ERROR-CODE                     
                 MOVE WS-DB-OPERATION-MSG
                                       TO OMGPR-ERROR-MESSAGE                   
                 MOVE 'Y'              TO OMGPR-F-PRICER-ERROR                  
           END-EVALUATE.                                                


SJ1022 3200-EXIT.
           EXIT. 
SJ1022 3300-GET-GRP-SAN-DATA.
           MOVE WS-PCTYPE-GRP-SAN-GS   TO CUPC-COMP-SHORT-CODE.
           PERFORM 5000-SQL-SELECT-PRICE-COMP THROUGH 5000-EXIT.

           EVALUATE SQLCODE
              WHEN 0
                 MOVE CUPC-COMP-SHORT-CODE 
                                       TO OMGPR-FEE-SHRT-CODE-GS 
                 MOVE CUPC-SKU-CODE    TO OMGPR-SKU-CODE-GS
                 MOVE CUPC-BILLING-FREQ
                                       TO OMGPR-BILLING-FRQ-GS
              WHEN 100
                 MOVE SPACES           TO OMGPR-FEE-SHRT-CODE-GS
                                          OMGPR-SKU-CODE-GS
                                          OMGPR-BILLING-FRQ-GS
              WHEN OTHER
                 MOVE 'SELECT'         TO WS-DB-OPERATION                       
                 MOVE 'CUTPRICE_COMPONENT'
                                       TO WS-DB-NAME
                 MOVE SQLCODE          TO WS-DB-RETURN-CODE                     
                 MOVE 'DB ERROR'       TO WS-DB-STATUS-MSG                      
                 MOVE 330              TO WS-DB-ERROR-NBR                      
                                          OMGPR-Q-ERROR-NBR                     
                 MOVE WS-DB2-FATAL-ERROR-IND
                                       TO OMGPR-Q-ERROR-CODE                     
                 MOVE WS-DB-OPERATION-MSG
                                       TO OMGPR-ERROR-MESSAGE                   
                 MOVE 'Y'              TO OMGPR-F-PRICER-ERROR                  
           END-EVALUATE.                                                


SJ1022 3300-EXIT.
           EXIT. 

SJ1022 3400-GET-GRP-NSC-DATA.
           MOVE WS-PCTYPE-GRP-NSC-GN   TO CUPC-COMP-SHORT-CODE.
           PERFORM 5000-SQL-SELECT-PRICE-COMP THROUGH 5000-EXIT.

           EVALUATE SQLCODE
              WHEN 0
                 MOVE CUPC-COMP-SHORT-CODE 
                                       TO OMGPR-FEE-SHRT-CODE-GN 
                 MOVE CUPC-SKU-CODE    TO OMGPR-SKU-CODE-GN
                 MOVE CUPC-BILLING-FREQ
                                       TO OMGPR-BILLING-FRQ-GN
              WHEN 100
                 MOVE SPACES           TO OMGPR-FEE-SHRT-CODE-GN
                                          OMGPR-SKU-CODE-GN
                                          OMGPR-BILLING-FRQ-GN
              WHEN OTHER
                 MOVE 'SELECT'         TO WS-DB-OPERATION                       
                 MOVE 'CUTPRICE_COMPONENT'
                                       TO WS-DB-NAME
                 MOVE SQLCODE          TO WS-DB-RETURN-CODE                     
                 MOVE 'DB ERROR'       TO WS-DB-STATUS-MSG                      
                 MOVE 340              TO WS-DB-ERROR-NBR                      
                                          OMGPR-Q-ERROR-NBR                     
                 MOVE WS-DB2-FATAL-ERROR-IND
                                       TO OMGPR-Q-ERROR-CODE                     
                 MOVE WS-DB-OPERATION-MSG
                                       TO OMGPR-ERROR-MESSAGE                   
                 MOVE 'Y'              TO OMGPR-F-PRICER-ERROR                  
           END-EVALUATE.                                                


SJ1022 3400-EXIT.
           EXIT.

SJ1022 3500-GET-IND-DATA.
           MOVE WS-PCTYPE-IND-MI       TO CUPC-COMP-SHORT-CODE.
           PERFORM 5000-SQL-SELECT-PRICE-COMP THROUGH 5000-EXIT.

           EVALUATE SQLCODE
              WHEN 0
                 MOVE CUPC-COMP-SHORT-CODE 
                                       TO OMGPR-FEE-SHRT-CODE-MI 
                 MOVE CUPC-SKU-CODE    TO OMGPR-SKU-CODE-MI
                 MOVE CUPC-BILLING-FREQ
                                       TO OMGPR-BILLING-FRQ-MI
              WHEN 100
                 MOVE SPACES           TO OMGPR-FEE-SHRT-CODE-MI
                                          OMGPR-SKU-CODE-MI
                                          OMGPR-BILLING-FRQ-MI
              WHEN OTHER
                 MOVE 'SELECT'         TO WS-DB-OPERATION                       
                 MOVE 'CUTPRICE_COMPONENT'
                                       TO WS-DB-NAME
                 MOVE SQLCODE          TO WS-DB-RETURN-CODE                     
                 MOVE 'DB ERROR'       TO WS-DB-STATUS-MSG                      
                 MOVE 350              TO WS-DB-ERROR-NBR                      
                                          OMGPR-Q-ERROR-NBR                     
                 MOVE WS-DB2-FATAL-ERROR-IND
                                       TO OMGPR-Q-ERROR-CODE                     
                 MOVE WS-DB-OPERATION-MSG
                                       TO OMGPR-ERROR-MESSAGE                   
                 MOVE 'Y'              TO OMGPR-F-PRICER-ERROR                  
           END-EVALUATE.                                                


SJ1022 3500-EXIT.
           EXIT. 
SJ1022 3600-GET-NON-CON-DATA.
           MOVE WS-PCTYPE-NON-CON-MN   TO CUPC-COMP-SHORT-CODE.
           PERFORM 5000-SQL-SELECT-PRICE-COMP THROUGH 5000-EXIT.

           EVALUATE SQLCODE
              WHEN 0
                 MOVE CUPC-COMP-SHORT-CODE 
                                       TO OMGPR-FEE-SHRT-CODE-MN 
                 MOVE CUPC-SKU-CODE    TO OMGPR-SKU-CODE-MN
                 MOVE CUPC-BILLING-FREQ
                                       TO OMGPR-BILLING-FRQ-MN
              WHEN 100
                 MOVE SPACES           TO OMGPR-FEE-SHRT-CODE-MN
                                          OMGPR-SKU-CODE-MN
                                          OMGPR-BILLING-FRQ-MN
              WHEN OTHER
                 MOVE 'SELECT'         TO WS-DB-OPERATION                       
                 MOVE 'CUTPRICE_COMPONENT'
                                       TO WS-DB-NAME
                 MOVE SQLCODE          TO WS-DB-RETURN-CODE                     
                 MOVE 'DB ERROR'       TO WS-DB-STATUS-MSG                      
                 MOVE 360              TO WS-DB-ERROR-NBR                     
                                          OMGPR-Q-ERROR-NBR                     
                 MOVE WS-DB2-FATAL-ERROR-IND
                                       TO OMGPR-Q-ERROR-CODE                     
                 MOVE WS-DB-OPERATION-MSG
                                       TO OMGPR-ERROR-MESSAGE                   
                 MOVE 'Y'              TO OMGPR-F-PRICER-ERROR                  
           END-EVALUATE.                                                


SJ1022 3600-EXIT.
           EXIT. 

SJ1022 3700-GET-CUS-DATA.
           MOVE WS-PCTYPE-CUS-MC       TO CUPC-COMP-SHORT-CODE.
           PERFORM 5000-SQL-SELECT-PRICE-COMP THROUGH 5000-EXIT.

           EVALUATE SQLCODE
              WHEN 0
                 MOVE CUPC-COMP-SHORT-CODE 
                                       TO OMGPR-FEE-SHRT-CODE-MC 
                 MOVE CUPC-SKU-CODE    TO OMGPR-SKU-CODE-MC
                 MOVE CUPC-BILLING-FREQ
                                       TO OMGPR-BILLING-FRQ-MC
              WHEN 100
                 MOVE SPACES           TO OMGPR-FEE-SHRT-CODE-MC
                                          OMGPR-SKU-CODE-MC
                                          OMGPR-BILLING-FRQ-MC
              WHEN OTHER
                 MOVE 'SELECT'         TO WS-DB-OPERATION                       
                 MOVE 'CUTPRICE_COMPONENT'
                                       TO WS-DB-NAME
                 MOVE SQLCODE          TO WS-DB-RETURN-CODE                     
                 MOVE 'DB ERROR'       TO WS-DB-STATUS-MSG                      
                 MOVE 370              TO WS-DB-ERROR-NBR                     
                                          OMGPR-Q-ERROR-NBR                     
                 MOVE WS-DB2-FATAL-ERROR-IND
                                       TO OMGPR-Q-ERROR-CODE                     
                 MOVE WS-DB-OPERATION-MSG
                                       TO OMGPR-ERROR-MESSAGE                   
                 MOVE 'Y'              TO OMGPR-F-PRICER-ERROR                  
           END-EVALUATE.                                                


SJ1022 3700-EXIT.
           EXIT. 

SJ1022 4000-SQL-SELECT-CUTFEE-PRICE.
           EXEC SQL
                 SELECT
                       FEE_SHORT_CODE,
                       FEE_TYPE,
                       FEE_PCT,
                       FEE_AMT,
                       SKU_CODE,
                       BILLING_FREQ,
                       CONVERT(CHAR(10),D_FEE_EXPIRE,101)
                 INTO
                       :CUFP-FEE-SHORT-CODE,
                       :CUFP-FEE-TYPE,
                       :CUFP-FEE-PCT,
                       :CUFP-FEE-AMT,
                       :CUFP-SKU-CODE,
                       :CUFP-BILLING-FREQ,
                       :CUFP-DATE-EXPIRE
                       :CUFP-D-ADJ-EXP-NI
                 FROM CUTFEE_PRICE
                 WHERE COMPANY_ID = :CUFP-COMPANY-ID AND 
                       CUST_ID = :WS-CUST-ID AND
                       SHIP_TO_SUFFIX = :CUFP-SHIP-TO-SUFFIX AND
                       FEE_SHORT_CODE = :CUFP-FEE-SHORT-CODE AND
                       D_FEE_EFFECTIVE <= :OMGPR-D-PRICING AND
                       (D_FEE_EXPIRE >= :OMGPR-D-PRICING OR
                       D_FEE_EXPIRE IS NULL)
           END-EXEC.

SJ1022 4000-EXIT.
           EXIT.


SJ1022 5000-SQL-SELECT-PRICE-COMP.
           EXEC SQL
              SELECT
                 COMPONENT_SHORT_CODE,
                 SKU_CODE,
                 BILLING_FREQ
              INTO
                 :CUPC-COMP-SHORT-CODE,
                 :CUPC-SKU-CODE,
                 :CUPC-BILLING-FREQ
              FROM CUTPRICE_COMPONENT
             WHERE COMPANY_ID = :CUPC-COMPANY-ID AND
                   CUST_ID = :WS-CUST-ID AND
                   SHIP_TO_SUFFIX = :CUPC-SHIP-TO-SUFFIX AND
                   COMPONENT_SHORT_CODE = :CUPC-COMP-SHORT-CODE AND
                   D_FEE_EFFECTIVE <= :OMGPR-D-PRICING AND
                   (D_FEE_EXPIRE >= :OMGPR-D-PRICING OR
                    D_FEE_EXPIRE IS NULL)
           END-EXEC.

SJ1022 5000-EXIT.
           EXIT.
