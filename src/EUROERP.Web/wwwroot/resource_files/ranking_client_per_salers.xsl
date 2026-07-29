<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:decimal-format decimal-separator="," grouping-separator="." NaN="0"/>
    <xsl:key name="rows_client" match="ds" use="UserName" />
    <xsl:template match="/DATA">
        <br/>
        <table border="0" width="600px">
            <tr>
                <td></td>
                <td width="200px">
                    <b>Cliente</b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="DATES/DATE[1]/MONTH_NAME"/>
                    </b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="DATES/DATE[2]/MONTH_NAME"/>
                    </b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="DATES/DATE[3]/MONTH_NAME"/>
                    </b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="DATES/DATE[4]/MONTH_NAME"/>
                    </b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="DATES/DATE[5]/MONTH_NAME"/>
                    </b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="DATES/DATE[6]/MONTH_NAME"/>
                    </b>
                </td>
                <td align="right">
                    <b>
                        Deve R$
                    </b>
                </td>
            </tr>
            <xsl:for-each select="Clients/NewDataSet/ds">
                <xsl:variable name="month1" select="/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[1]/MONTH and Y=/DATA/DATES/DATE[1]/YEAR and CID=current()/PKId]/TO"></xsl:variable>
                <xsl:variable name="month2" select="/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[2]/MONTH and Y=/DATA/DATES/DATE[2]/YEAR and CID=current()/PKId]/TO"></xsl:variable>
                <xsl:variable name="month3" select="/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[3]/MONTH and Y=/DATA/DATES/DATE[3]/YEAR and CID=current()/PKId]/TO"></xsl:variable>
                <xsl:variable name="month4" select="/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[4]/MONTH and Y=/DATA/DATES/DATE[4]/YEAR and CID=current()/PKId]/TO"></xsl:variable>
                <xsl:variable name="month5" select="/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[5]/MONTH and Y=/DATA/DATES/DATE[5]/YEAR and CID=current()/PKId]/TO"></xsl:variable>
                <xsl:variable name="month6" select="/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[6]/MONTH and Y=/DATA/DATES/DATE[6]/YEAR and CID=current()/PKId]/TO"></xsl:variable>
                <xsl:variable name="deve" select="current()/BAL"></xsl:variable>
                <tr>
                    <xsl:attribute name="style">
                        <xsl:if test="$deve &gt;0">
                            color:Red
                        </xsl:if>
                    </xsl:attribute>
                    <xsl:if test="(position() mod 2 = 1)">
                        <xsl:attribute name="bgcolor">#CDCDCD</xsl:attribute>
                    </xsl:if>
                    <td align="right">
                        <xsl:value-of select="position()"/>
                    </td>
                    <td align="left" width="200px">
                        <a href="#">
                            <xsl:attribute name="style">
                                <xsl:if test="$deve &gt;0">
                                    color:Red
                                </xsl:if>
                            </xsl:attribute>
                            <xsl:attribute name="onClick">
                                window.open('/principal/clientes/cadastro/<xsl:value-of select="PKId"/>','','height=600,width=500,left=300,top=50,scrollbars=1');return false;
                            </xsl:attribute>
                            <xsl:value-of select="substring(FANTASY_NAME,0,31)"/>
                        </a>
                    </td>
                    <td align="right">
                        <a href="#">
                            <xsl:attribute name="style">
                                <xsl:if test="$deve &gt;0">
                                    color:Red
                                </xsl:if>
                            </xsl:attribute>
                            <xsl:attribute name="onClick">
                                window.open('/vendas/relatorios/abc/clientes/mes?clientId=<xsl:value-of select="PKId"/>&amp;M=<xsl:value-of select="/DATA/DATES/DATE[1]/MONTH"/>&amp;Y=<xsl:value-of select="/DATA/DATES/DATE[1]/YEAR"/>','','height=600,width=650,left=300,top=50,scrollbars=1');return false;
                            </xsl:attribute>
                            <xsl:value-of select="format-number($month1,'##.##0,00')"/>
                        </a>
                    </td>
                    <td align="right">
                        <a href="#">
                            <xsl:attribute name="style">
                                <xsl:if test="$deve &gt;0">
                                    color:Red
                                </xsl:if>
                            </xsl:attribute>
                            <xsl:attribute name="onClick">
                                window.open('/vendas/relatorios/abc/clientes/mes?clientId=<xsl:value-of select="PKId"/>&amp;M=<xsl:value-of select="/DATA/DATES/DATE[2]/MONTH"/>&amp;Y=<xsl:value-of select="/DATA/DATES/DATE[2]/YEAR"/>','','height=600,width=650,left=300,top=50,scrollbars=1');return false;
                            </xsl:attribute>
                            <xsl:value-of select="format-number($month2,'##.##0,00')"/>
                        </a>
                    </td>
                    <td align="right">
                        <a href="#">
                            <xsl:attribute name="style">
                                <xsl:if test="$deve &gt;0">
                                    color:Red
                                </xsl:if>
                            </xsl:attribute>
                            <xsl:attribute name="onClick">
                                window.open('/vendas/relatorios/abc/clientes/mes?clientId=<xsl:value-of select="PKId"/>&amp;M=<xsl:value-of select="/DATA/DATES/DATE[3]/MONTH"/>&amp;Y=<xsl:value-of select="/DATA/DATES/DATE[3]/YEAR"/>','','height=600,width=650,left=300,top=50,scrollbars=1');return false;
                            </xsl:attribute>
                            <xsl:value-of select="format-number($month3,'##.##0,00')"/>
                        </a>
                    </td>
                    <td align="right">
                        <a href="#">
                            <xsl:attribute name="style">
                                <xsl:if test="$deve &gt;0">
                                    color:Red
                                </xsl:if>
                            </xsl:attribute>
                            <xsl:attribute name="onClick">
                                window.open('/vendas/relatorios/abc/clientes/mes?clientId=<xsl:value-of select="PKId"/>&amp;M=<xsl:value-of select="/DATA/DATES/DATE[4]/MONTH"/>&amp;Y=<xsl:value-of select="/DATA/DATES/DATE[4]/YEAR"/>','','height=600,width=650,left=300,top=50,scrollbars=1');return false;
                            </xsl:attribute>
                            <xsl:value-of select="format-number($month4,'##.##0,00')"/>
                        </a>
                    </td>
                    <td align="right">
                        <a href="#">
                            <xsl:attribute name="style">
                                <xsl:if test="$deve &gt;0">
                                    color:Red
                                </xsl:if>
                            </xsl:attribute>
                            <xsl:attribute name="onClick">
                                window.open('/vendas/relatorios/abc/clientes/mes?clientId=<xsl:value-of select="PKId"/>&amp;M=<xsl:value-of select="/DATA/DATES/DATE[5]/MONTH"/>&amp;Y=<xsl:value-of select="/DATA/DATES/DATE[5]/YEAR"/>','','height=600,width=650,left=300,top=50,scrollbars=1');return false;
                            </xsl:attribute>
                            <xsl:value-of select="format-number($month5,'##.##0,00')"/>
                        </a>
                    </td>
                    <td align="right">
                        <a href="#">
                            <xsl:attribute name="style">
                                <xsl:if test="$deve &gt;0">
                                    color:Red
                                </xsl:if>
                            </xsl:attribute>
                            <xsl:attribute name="onClick">
                                window.open('/vendas/relatorios/abc/clientes/mes?clientId=<xsl:value-of select="PKId"/>&amp;M=<xsl:value-of select="/DATA/DATES/DATE[6]/MONTH"/>&amp;Y=<xsl:value-of select="/DATA/DATES/DATE[6]/YEAR"/>','','height=600,width=650,left=300,top=50,scrollbars=1');return false;
                            </xsl:attribute>
                            <xsl:value-of select="format-number($month6,'##.##0,00')"/>
                        </a>
                    </td>
                    <td align="right">
                        <a href="#">
                            <xsl:attribute name="style">
                                <xsl:if test="$deve &gt;0">
                                    color:Red
                                </xsl:if>
                            </xsl:attribute>
                            <xsl:attribute name="onClick">
                                <xsl:choose>
                                    <xsl:when  test="$deve &gt; 0">
                                        window.open('/financeiro/contas-a-receber?cid=<xsl:value-of select="PKId"/>&amp;nonpaid=Y&amp;masterPageDef=nomenu','','height=400,width=800,left=100,top=50,resizable=1,scrollbars=1');return false;
                                    </xsl:when>
                                    <xsl:otherwise>return false;</xsl:otherwise>
                                </xsl:choose>
                            </xsl:attribute>
                            <xsl:value-of select="format-number($deve,'##.##0,00')"/>
                        </a>
                    </td>
                </tr>
            </xsl:for-each>
            <xsl:variable name="v1" select="count(/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[1]/MONTH and Y=/DATA/DATES/DATE[1]/YEAR and TO &gt; 0])"></xsl:variable>
            <xsl:variable name="v2" select="count(/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[2]/MONTH and Y=/DATA/DATES/DATE[2]/YEAR and TO &gt; 0])"></xsl:variable>
            <xsl:variable name="v3" select="count(/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[3]/MONTH and Y=/DATA/DATES/DATE[3]/YEAR and TO &gt; 0])"></xsl:variable>
            <xsl:variable name="v4" select="count(/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[4]/MONTH and Y=/DATA/DATES/DATE[4]/YEAR and TO &gt; 0])"></xsl:variable>
            <xsl:variable name="v5" select="count(/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[5]/MONTH and Y=/DATA/DATES/DATE[5]/YEAR and TO &gt; 0])"></xsl:variable>
            <xsl:variable name="v6" select="count(/DATA/Results/NewDataSet/ds[M=/DATA/DATES/DATE[6]/MONTH and Y=/DATA/DATES/DATE[6]/YEAR and TO &gt; 0])"></xsl:variable>
            <xsl:variable name="t1" select="count(/DATA/Clients/NewDataSet/ds)"></xsl:variable>
            <tr>
                <td></td>
                <td align="right">
                    <b>Aproveitamento</b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="format-number(($v1 div $t1)*100,'##.##0,00')"/>%
                    </b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="format-number(($v2 div $t1)*100,'##.##0,00')"/>%
                    </b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="format-number(($v3 div $t1)*100,'##.##0,00')"/>%
                    </b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="format-number(($v4 div $t1)*100,'##.##0,00')"/>%
                    </b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="format-number(($v5 div $t1)*100,'##.##0,00')"/>%
                    </b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="format-number(($v6 div $t1)*100,'##.##0,00')"/>%
                    </b>
                </td>
            </tr>
            <tr>
                <td colspan="3">
                    <hr/>
                </td>
                <td colspan="5"></td>
            </tr>
            <tr>
                <td colspan="2" align="right">
                    <b>Aproveitamento Geral</b>
                </td>
                <td align="right">
                    <b>
                        <xsl:value-of select="format-number((($v1+$v2+$v3+$v4+$v5+$v6) div $t1 div 6)*100,'##.##0,00')"/>%
                    </b>
                </td>
                <td colspan="5"></td>
            </tr>
        </table>
    </xsl:template>
</xsl:stylesheet>