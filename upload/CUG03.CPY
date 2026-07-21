      **  Modification Log:                                       **
      **     DATE     PRGM      DESCRIPTION                       **
      *-----------------------------------------------------------**
SJ0421*  04/01/2021   SJACOB    WR 20-9002 PRICER ENHANCEMENT     **
SJ0421*-----------------------------------------------------------**
001480 01  CUG03.                                                       00000001
001490     05 CUG03-C.                                                  00000002
001500        10 CUG03-CU-ACCOUNT.                                      00000003
001510           15 CUG03-I-ACCOUNT                   PIC S9(8)         00000004
001520                                     VALUE     ZERO               00000005
001530                                     COMPUTATIONAL.               00000006
001540           15 CUG03-I-DIVISION                  PIC X(2)          00000007
001550                                     VALUE     SPACE.             00000008
001560           15 CUG03-S-ACCOUNT                   PIC X(6)          00000009
001570                                     VALUE     SPACE.             00000010
001580           15 CUG03-C-ACCT-STATUS               PIC X(2)          00000011
001590                                     VALUE     SPACE.             00000012
001600           15 CUG03-D-ACCT-STATUS               PIC X(10)         00000013
001610                                     VALUE     SPACE.             00000014
001620           15 CUG03-D-ACCT-STATUS-FULL                            00000015
001630                                     REDEFINES                    00000016
001640              CUG03-D-ACCT-STATUS.                                00000017
001650           16 CUG03-D-ACCT-STATUS-MO-X.                           00000018
001660           17 CUG03-D-ACCT-STATUS-MO            PIC 9(2).         00000019
001670           16 CUG03-D-ACCT-STATUS-S1            PIC X(1).         00000020
001680           16 CUG03-D-ACCT-STATUS-DY-X.                           00000021
001690           17 CUG03-D-ACCT-STATUS-DY            PIC 9(2).         00000022
001700           16 CUG03-D-ACCT-STATUS-S2            PIC X(1).         00000023
001710           16 CUG03-D-ACCT-STATUS-YR-X.                           00000024
001720           17 CUG03-D-ACCT-STATUS-YR            PIC 9(4).         00000025
001730           15 CUG03-I-ACCT-DEA-NBR              PIC X(9)          00000026
001740                                     VALUE     SPACE.             00000027
001750           15 CUG03-I-ACCT-OMNIPLUS             PIC X(4)          00000028
001760                                     VALUE     SPACE.             00000029
001770           15 CUG03-C-ACCT-CREDIT               PIC X(1)          00000030
001780                                     VALUE     SPACE.             00000031
001790           15 CUG03-Q-ACCT-CREDIT-LIM           PIC S9(7)         00000032
001800                                     VALUE     ZERO               00000033
001810                                     COMPUTATIONAL.               00000034
001820           15 CUG03-F-ACCT-CORP-CR-LIM          PIC X(1)          00000035
001830                                     VALUE     SPACE.             00000036
001840           15 CUG03-F-ACCT-SVC-CHARGE           PIC X(1)          00000037
001850                                     VALUE     SPACE.             00000038
001860           15 CUG03-A-ACCT-SVC-CRG-PCT          PIC SV9(3)        00000039
001870                                     VALUE     ZERO               00000040
001880                                     COMPUTATIONAL-3.             00000041
001890           15 CUG03-Q-ACCT-ORD-CR-LIM           PIC S9(7)         00000042
001900                                     VALUE     ZERO               00000043
001910                                     COMPUTATIONAL.               00000044
001920           15 CUG03-C-ACCT-PAY-PERIOD           PIC X(1)          00000045
001930                                     VALUE     SPACE.             00000046
001940           15 CUG03-C-ACCT-MOD-10-11            PIC X(1)          00000047
001950                                     VALUE     SPACE.             00000048
001960           15 CUG03-C-ACCT-SHP-DOC-SEQ          PIC X(1)          00000049
001970                                     VALUE     SPACE.             00000050
001980           15 CUG03-C-ACCT-STMT-PRINT           PIC X(1)          00000051
001990                                     VALUE     SPACE.             00000052
002000           15 CUG03-C-ACCT-SI-DETAIL            PIC X(1)          00000053
002010                                     VALUE     SPACE.             00000054
002020           15 CUG03-F-ACCT-SUM-INV-PRT          PIC X(1)          00000055
002030                                     VALUE     SPACE.             00000056
002040           15 CUG03-F-ACCT-SI-SUMM-REC          PIC X(1)          00000057
002050                                     VALUE     SPACE.             00000058
002060           15 CUG03-C-ACCT-SUM-INVOICE          PIC X(1)          00000059
002070                                     VALUE     SPACE.             00000060
002080           15 CUG03-F-ACCT-APPLY-CASH           PIC X(1)          00000061
002090                                     VALUE     SPACE.             00000062
002100           15 CUG03-F-ACCT-APPLY-CHECK          PIC X(1)          00000063
002110                                     VALUE     SPACE.             00000064
002120           15 CUG03-F-ACCT-PART-USE             PIC X(1)          00000065
002130                                     VALUE     SPACE.             00000066
002140           15 CUG03-F-ACCT-SAVE-DESC            PIC X(1)          00000067
002150                                     VALUE     SPACE.             00000068
002160           15 CUG03-F-ACCT-XREF-ITEM            PIC X(1)          00000069
002170                                     VALUE     SPACE.             00000070
002180           15 CUG03-I-ACCT-PT-XRF-DIV           PIC X(2)          00000071
002190                                     VALUE     SPACE.             00000072
002200           15 CUG03-I-ACCT-PT-XRF-ACCT          PIC X(6)          00000073
002210                                     VALUE     SPACE.             00000074
002220           15 CUG03-C-ACCT-DISP-FIELD           PIC X(1)          00000075
002230                                     VALUE     SPACE.             00000076
002240           15 CUG03-T-ACCT-DISP-1-HDR           PIC X(6)          00000077
002250                                     VALUE     SPACE.             00000078
002260           15 CUG03-T-ACCT-DISP-2-HDR           PIC X(4)          00000079
002270                                     VALUE     SPACE.             00000080
002280           15 CUG03-C-ACCT-PRCE-METHOD          PIC X(2)          00000081
002290                                     VALUE     SPACE.             00000082
002300     88 CUG03-USAGE-PRCE-METHOD                                   00000083
002310                                     VALUE     'AU'.              00000084
002320     88 CUG03-STOCK-PRCE-METHOD                                   00000085
002330                                     VALUE     'ST'.              00000086
002340     88 CUG03-CONT-PRCE-METHOD                                    00000087
002350                                     VALUE     'CC'.              00000088
002360           15 CUG03-C-ACCT-ROUNDING             PIC X(1)          00000089
002370                                     VALUE     SPACE.             00000090
002380     88 CUG03-NORMAL-TWO-ROUNDING                                 00000091
002390                                     VALUE     'R'.               00000092
002400     88 CUG03-UP-TWO-ROUNDING                                     00000093
002410                                     VALUE     'Y'.               00000094
002420     88 CUG03-NO-ROUNDING                                         00000095
002430                                     VALUE     'N'                00000096
002440     ' '                                                          00000097
002450     .                                                            00000098
002460           15 CUG03-P-ACCT-EXPECTED-GM          PIC S9(1)V9(4)    00000099
002470                                     VALUE     ZERO               00000100
002480                                     COMPUTATIONAL-3.             00000101
002490           15 CUG03-I-CUSTOMER                  PIC S9(8)         00000102
002500                                     VALUE     ZERO               00000103
002510                                     COMPUTATIONAL.               00000104
002520           15 CUG03-N-ACCT-SORT                 PIC X(8)          00000105
002530                                     VALUE     SPACE.             00000106
002540           15 CUG03-N-ACCT                      PIC X(35)         00000107
002550                                     VALUE     SPACE.             00000108
002280           15 CUG03-C-ACCT-MATCH-IND            PIC X(2)          00000109
002290                                     VALUE     SPACE.             00000110
SJ0421           15 CUG03-CUSTOMER-NBR                PIC S9(10)
SJ0421                                     VALUE      ZERO
SJ0421                                     COMPUTATIONAL-3.
002560           EXEC SQL                                               00000111
002570             DECLARE CUG03 TABLE                                  00000112
002580                 (I_ACCOUNT            INTEGER                    00000113
002590                                       NOT NULL,                  00000114
002600                  I_DIVISION           CHAR(002)                  00000115
002610                                       NOT NULL,                  00000116
002620                  S_ACCOUNT            CHAR(006)                  00000117
002630                                       NOT NULL,                  00000118
002640                  C_ACCT_STATUS        CHAR(002)                  00000119
002650                                       NOT NULL,                  00000120
002660                  D_ACCT_STATUS        DATE                       00000121
002670                                       NOT NULL,                  00000122
002680                  I_ACCT_DEA_NBR       CHAR(009)                  00000123
002690                                       NOT NULL,                  00000124
002700                  I_ACCT_OMNIPLUS      CHAR(004)                  00000125
002710                                       NOT NULL,                  00000126
002720                  C_ACCT_CREDIT        CHAR(001)                  00000127
002730                                       NOT NULL,                  00000128
002740                  Q_ACCT_CREDIT_LIM    INTEGER                    00000129
002750                                       NOT NULL,                  00000130
002760                  F_ACCT_CORP_CR_LIM   CHAR(001)                  00000131
002770                                       NOT NULL,                  00000132
002780                  F_ACCT_SVC_CHARGE    CHAR(001)                  00000133
002790                                       NOT NULL,                  00000134
002800                  A_ACCT_SVC_CRG_PCT   DECIMAL(003,03)            00000135
002810                                       NOT NULL,                  00000136
002820                  Q_ACCT_ORD_CR_LIM    INTEGER                    00000137
002830                                       NOT NULL,                  00000138
002840                  C_ACCT_PAY_PERIOD    CHAR(001)                  00000139
002850                                       NOT NULL,                  00000140
002860                  C_ACCT_MOD_10_11     CHAR(001)                  00000141
002870                                       NOT NULL,                  00000142
002880                  C_ACCT_SHP_DOC_SEQ   CHAR(001)                  00000143
002890                                       NOT NULL,                  00000144
002900                  C_ACCT_STMT_PRINT    CHAR(001)                  00000145
002910                                       NOT NULL,                  00000146
002920                  C_ACCT_SI_DETAIL     CHAR(001)                  00000147
002930                                       NOT NULL,                  00000148
002940                  F_ACCT_SUM_INV_PRT   CHAR(001)                  00000149
002950                                       NOT NULL,                  00000150
002960                  F_ACCT_SI_SUMM_REC   CHAR(001)                  00000151
002970                                       NOT NULL,                  00000152
002980                  C_ACCT_SUM_INVOICE   CHAR(001)                  00000153
002990                                       NOT NULL,                  00000154
003000                  F_ACCT_APPLY_CASH    CHAR(001)                  00000155
003010                                       NOT NULL,                  00000156
003020                  F_ACCT_APPLY_CHECK   CHAR(001)                  00000157
003030                                       NOT NULL,                  00000158
003040                  F_ACCT_PART_USE      CHAR(001)                  00000159
003050                                       NOT NULL,                  00000160
003060                  F_ACCT_SAVE_DESC     CHAR(001)                  00000161
003070                                       NOT NULL,                  00000162
003080                  F_ACCT_XREF_ITEM     CHAR(001)                  00000163
003090                                       NOT NULL,                  00000164
003100                  I_ACCT_PT_XRF_DIV    CHAR(002)                  00000165
003110                                       NOT NULL,                  00000166
003120                  I_ACCT_PT_XRF_ACCT   CHAR(006)                  00000167
003130                                       NOT NULL,                  00000168
003140                  C_ACCT_DISP_FIELD    CHAR(001)                  00000169
003150                                       NOT NULL,                  00000170
003160                  T_ACCT_DISP_1_HDR    CHAR(006)                  00000171
003170                                       NOT NULL,                  00000172
003180                  T_ACCT_DISP_2_HDR    CHAR(004)                  00000173
003190                                       NOT NULL,                  00000174
003200                  C_ACCT_PRCE_METHOD   CHAR(002)                  00000175
003210                                       NOT NULL,                  00000176
003220                  C_ACCT_ROUNDING      CHAR(001)                  00000177
003230                                       NOT NULL,                  00000178
003240                  P_ACCT_EXPECTED_GM   DECIMAL(005,04)            00000179
003250                                       NOT NULL,                  00000180
003260                  I_CUSTOMER           INTEGER                    00000181
003270                                       NOT NULL,                  00000182
003280                  N_ACCT_SORT          CHAR(008)                  00000183
003290                                       NOT NULL,                  00000184
003300                  N_ACCT               CHAR(035)                  00000185
003310                                       NOT NULL,                  00000186
003200                  C_ACCT_MATCH_IND     CHAR(002)                  00000187
003210                                       NOT NULL,                  00000188
SJ0421                  CUSTOMER_NBR         DECIMAL(10,0)
SJ0421                                       NOT NULL)
003320           END-EXEC.                                              00000189
003330 01  CUG03-NULL-INDICATORS.                                       00000190
003340        10 CUG03-NO-NULL-VARIABLES              PIC S9(4)         00000191
003350                                     VALUE     +0000              00000192
003360                                     COMPUTATIONAL.               00000200
