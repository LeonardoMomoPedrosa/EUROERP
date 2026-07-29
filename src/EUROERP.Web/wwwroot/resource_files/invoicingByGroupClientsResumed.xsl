<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:decimal-format decimal-separator="," grouping-separator="."/>
    <xsl:key name="rows_client" match="ds" use="CID" />
    <xsl:key name="rows_group" match="ds" use="GID" />
    <xsl:key name="rows_group_client" match="ds" use="concat(CID,'_',GROUP)" />
    <xsl:param name="detailPage"/>
    <xsl:param name="userName"/>
    <xsl:param name="first"/>
    <xsl:param name="last"/>
    <xsl:template match="/GROUP_REPORT">
        <br/>
        <div align="left">
            <xsl:variable name="main_total" select="sum(NewDataSet/ds/PRICE)"/>
            <table border="0" width="800">
                <tr>
                    <td align="left">
                        <b>Cliente</b>
                    </td>
                    <td align="center">
                        <b>Vendedor</b>
                    </td>
                    <td align="right">
                        <b>Total (R$)</b>
                    </td>
                    <td align="right">
                        <b>Peso</b>
                    </td>
                </tr>
                <xsl:for-each select="NewDataSet/ds[count(. | key('rows_client', CID)[1]) = 1]">
                    <xsl:sort select="sum(key('rows_client',CID)/PRICE)" data-type="number" order="descending"/>
                    <xsl:variable name="clients" select="key('rows_client',CID)"/>
                    <xsl:variable name="total_client" select="sum($clients/PRICE)"/>
                    <tr>
                        <td bgcolor="#E3E5A3">
                            <a href="#">
                                <xsl:attribute name="onClick">
                                    window.open('<xsl:value-of select="$detailPage"/>?clientId=<xsl:value-of select="CID"/>&amp;clientName=<xsl:value-of select="FN"/>&amp;userName=<xsl:value-of select="$userName"/>&amp;first=<xsl:value-of select="$first"/>&amp;last=<xsl:value-of select="$last"/>','','height=600,width=800,left=50,top=50,scrollbars=1,menubar=0');return false;
                                </xsl:attribute>
                                <xsl:value-of select="FN"/>
                            </a>
                        </td>
                        <td bgcolor="#E3E5A3" align="center">
                            <xsl:value-of select="OS"/>
                        </td>
                        <td bgcolor="#E3E5A3" align="right">
                            <xsl:value-of select="format-number($total_client,'##.##0,00')"/>
                        </td>
                        <td bgcolor="#E3E5A3" align="right">
                            <xsl:value-of select="format-number(($total_client div $main_total)*100,'##.##0,00')"/> %
                        </td>
                    </tr>
                </xsl:for-each>
                <tr bgcolor="#E3E5A3">
                    <td align="right">
                        <b>Total</b>
                    </td>
                    <td align="right">
                        <b>
                            R$ <xsl:value-of select="format-number($main_total,'##.##0,00')"/>
                        </b>
                    </td>
                    <td></td>
                </tr>
            </table>
        </div>
    </xsl:template>
</xsl:stylesheet>