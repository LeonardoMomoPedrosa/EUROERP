<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:param name="clientId"/>
    <xsl:param name="detailPage"/>
    <xsl:param name="userName"/>
    <xsl:param name="first"/>
    <xsl:param name="last"/>
    <xsl:decimal-format decimal-separator="," grouping-separator="." NaN="0,00"/>
    <xsl:key name="rows_client" match="ds" use="CID" />
    <xsl:key name="rows_group" match="ds" use="GID" />
    <xsl:key name="rows_group_client" match="ds" use="concat(CID,'_',GROUP)" />
    <xsl:template match="/GROUP_REPORT">
        <br/>
        <div align="left">
            <xsl:variable name="main_total" select="sum(NewDataSet/ds/PRICE)"/>
            <table border="0" width="500">
                <xsl:variable name="clients" select="key('rows_client',$clientId)"/>
                <xsl:variable name="refGroups" select="GroupRefDs/*"/>
                <xsl:variable name="total_client" select="sum($clients/PRICE)"/>
                <tr>
                    <td colspan="3" bgcolor="#E3E5A3">
                        <b>
                            &#160;<xsl:value-of select="FN"/>
                        </b>
                    </td>
                </tr>
                <tr>
                    <td align="left">
                        <b>GRUPO</b>
                    </td>
                    <td align="right">
                        <b>Total (R$)</b>
                    </td>
                    <td align="right">
                        <b>Peso</b>
                    </td>
                </tr>
                <tr bgcolor="#CDCDCD">
                    <td colspan="3" align="left">
                        <b>Produtos</b>
                    </td>
                </tr>
                <xsl:call-template name="main-tpl">
                    <xsl:with-param name="cid" select="CID"/>
                    <xsl:with-param name="total_client" select="$total_client"/>
                    <xsl:with-param name="dss" select="$clients"/>
                    <xsl:with-param name="refGroups" select="$refGroups[CLASS_ID=1]"/>
                    <xsl:with-param name="pcid" select="1"/>
                </xsl:call-template>
                <tr bgcolor="#CDCDCD">
                    <td colspan="3" align="left">
                        <b>Serviços</b>
                    </td>
                </tr>
                <xsl:call-template name="main-tpl">
                    <xsl:with-param name="cid" select="CID"/>
                    <xsl:with-param name="total_client" select="$total_client"/>
                    <xsl:with-param name="dss" select="$clients"/>
                    <xsl:with-param name="refGroups" select="$refGroups[CLASS_ID=2]"/>
                    <xsl:with-param name="pcid" select="2"/>
                </xsl:call-template>
                <tr>
                    <td align="right">
                        <b>Total</b>
                    </td>
                    <td align="right">
                        <b>
                            R$ <xsl:value-of select="format-number($total_client,'##.##0,00')"/>
                        </b>
                    </td>
                    <td align="right">
                        <b>
                            <xsl:value-of select="format-number(($total_client div $main_total)*100,'##.##0,00')"/> %
                        </b>
                    </td>
                </tr>
                <tr>
                    <td colspan="3"> </td>
                </tr>
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

    <xsl:template name="main-tpl">
        <xsl:param name="cid"/>
        <xsl:param name="total_client"/>
        <xsl:param name="dss"/>
        <xsl:param name="refGroups"/>
        <xsl:param name="pcid"/>
        <xsl:variable name="total" select="sum($dss[PC_ID=$pcid]/PRICE)"/>
        <xsl:for-each select="$refGroups">
            <xsl:sort select="sum($dss[GID=current()/PKId]/PRICE)" data-type="number" order="descending"/>
            <xsl:variable name="total_group" select="sum($dss[GID=current()/PKId]/PRICE)"/>
            <tr>
                <td align="left">
                    <a href="#">
                        <xsl:attribute name="onClick">
                            window.open('<xsl:value-of select="$detailPage"/>?groupId=<xsl:value-of select="PKId"/>&amp;clientId=<xsl:value-of select="$clientId"/>&amp;clientName=<xsl:value-of select="$dss[1]/FN"/>&amp;groupName=<xsl:value-of select="NAME"/>&amp;userName=<xsl:value-of select="$userName"/>&amp;first=<xsl:value-of select="$first"/>&amp;last=<xsl:value-of select="$last"/>','','height=600,width=800,left=100,top=50,scrollbars=1,menubar=0');return false;
                        </xsl:attribute>
                        <xsl:value-of select="NAME"/>
                    </a>
                </td>
                <td align="right">
                    <xsl:value-of select="format-number($total_group,'##.##0,00')"/>
                </td>
                <td align="right">
                    <xsl:value-of select="format-number(($total_group div $total)*100,'##.##0,00')"/> %
                </td>
            </tr>
        </xsl:for-each>
        <tr>
            <td align="right">
            </td>
            <td align="right">
                <b>
                    R$ <xsl:value-of select="format-number($total,'##.##0,00')"/>
                </b>
            </td>
            <td align="right">
                <b>
                    <xsl:value-of select="format-number(($total div $total_client)*100,'##.##0,00')"/> %
                </b>
            </td>
        </tr>
    </xsl:template>
</xsl:stylesheet>