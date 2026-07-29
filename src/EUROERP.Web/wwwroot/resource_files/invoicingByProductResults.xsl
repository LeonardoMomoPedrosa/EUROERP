<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:exsl="http://exslt.org/common"
                extension-element-prefixes="exsl"
                version="1.0">
    <xsl:param name="first"/>
    <xsl:param name="last"/>
    <xsl:param name="ordersListPage"/>
    <xsl:decimal-format decimal-separator="," grouping-separator="." NaN="0,00"/>
    <xsl:key name="rows_product" match="ds" use="PID" />
    <xsl:template match="/GROUP_REPORT">
        <br/>
        <div align="left">
            <xsl:variable name="main_total" select="sum(NewDataSet/ds/PRICE)"/>
            <xsl:variable name="sortedProducts">
                <xsl:for-each select="NewDataSet/ds[count(. | key('rows_product', PID)[1]) = 1 and HCI = 'true']">
                    <xsl:sort select="sum(/GROUP_REPORT/NewDataSet/ds[PID=current()/PID]/PRICE) - sum(/GROUP_REPORT/NewDataSet/ds[PID=current()/PID]/CF)" data-type="number" order="descending"/>
                    <xsl:variable name="total_products" select="sum(/GROUP_REPORT/NewDataSet/ds[PID=current()/PID]/PRICE)"/>
                    <xsl:variable name="total_qtd" select="sum(/GROUP_REPORT/NewDataSet/ds[PID=current()/PID]/QTD)"/>
                    <xsl:variable name="total_cost_saler" select="sum(/GROUP_REPORT/NewDataSet/ds[PID=current()/PID]/CF)"/>
                    <ITEM>
                        <PKId>
                            <xsl:value-of select="PID"/>
                        </PKId>
                        <QTD>
                            <xsl:value-of select="$total_qtd"/>
                        </QTD>
                        <NAME>
                            <xsl:value-of select="NAME"/>
                        </NAME>
                        <SIZE>
                            <xsl:value-of select="AZ"/>
                        </SIZE>
                        <TOTAL>
                            <xsl:value-of select="$total_products"/>
                        </TOTAL>
                        <TOTAL_COST>
                            <xsl:value-of select="$total_cost_saler"/>
                        </TOTAL_COST>
                        <PROFIT>
                            <xsl:value-of select="$total_products - $total_cost_saler"/>
                        </PROFIT>
                    </ITEM>
                </xsl:for-each>
            </xsl:variable>
            <table border="0" width="100%">
                <tr>
                    <td></td>
                    <td align="right">
                        <b>Cod.</b>
                    </td>
                    <td align="left">
                        <b>Produto</b>
                    </td>
                    <td align="right">
                        <b>Qtd.</b>
                    </td>
                    <td align="right">
                        <b>Total (R$)</b>
                    </td>
                    <td align="right">
                        <b>Custo (R$)</b>
                    </td>
                    <td align="right">
                        <b>Lucro %</b>
                    </td>
                    <td align="right">
                        <b>Lucro (R$)</b>
                    </td>
                    <td align="right">
                        <b>Peso</b>
                    </td>
                    <td align="right">
                        <b>Acum.</b>
                    </td>
                </tr>
                <xsl:for-each select="exsl:node-set($sortedProducts)/ITEM">
                    <xsl:variable name="total_product" select="TOTAL"/>
                    <xsl:variable name="total_cost_product" select="TOTAL_COST"/>
                    <xsl:variable name="total_profit" select="sum(exsl:node-set($sortedProducts)/ITEM/PROFIT)"/>
                    <xsl:variable name="currPos" select="position()"/>
                    <xsl:variable name="preAcum" select="sum(exsl:node-set($sortedProducts)/ITEM[position() &lt; $currPos]/PROFIT) div $total_profit * 100"/>
                    <xsl:variable name="acum" select="$preAcum + ($total_product - $total_cost_product) div $total_profit * 100"/>
                    <tr>
                        <xsl:if test="(position() mod 2 = 1)">
                            <xsl:attribute name="bgcolor">#CDCDCD</xsl:attribute>
                        </xsl:if>
                        <td width="3px">
                            <xsl:attribute name="bgcolor">
                                <xsl:choose>
                                    <xsl:when test="$preAcum &gt;= 95">orange</xsl:when>
                                    <xsl:when test="$preAcum &gt;= 80">green</xsl:when>
                                    <xsl:otherwise>blue</xsl:otherwise>
                                </xsl:choose>
                            </xsl:attribute>
                        </td>
                        <td align="right">
                            <xsl:value-of select="PKId"/>
                        </td>
                        <td>
                            <a href="#">
                                <xsl:attribute name="onClick">
                                    window.open('/vendas/relatorios/abc/grupo/pedidos?productId=<xsl:value-of select="PKId"/>&amp;productName=<xsl:value-of select="concat(NAME,' ',SIZE)"/>&amp;first=<xsl:value-of select="$first"/>&amp;last=<xsl:value-of select="$last"/>','','height=350,width=600,left=250,top=150,scrollbars=1,menubar=0');return false;
                                </xsl:attribute>
                                <xsl:value-of select="NAME"/>&#160;
                                <i>
                                    <xsl:value-of select="SIZE"/>
                                </i>
                            </a>
                        </td>
                        <td align="right">
                            <xsl:value-of select="QTD"/>
                        </td>
                        <td align="right">
                            <xsl:value-of select="format-number($total_product,'##.##0,00')"/>
                        </td>
                        <td align="right">
                            <xsl:value-of select="format-number($total_cost_product,'##.##0,00')"/>
                        </td>
                        <td align="right">
                            <xsl:value-of select="format-number(($total_product div $total_cost_product - 1)*100,'##.##0')"/> %
                        </td>
                        <td align="right">
                            <xsl:value-of select="format-number(PROFIT,'##.##0,00')"/>
                        </td>
                        <td align="right">
                            <xsl:value-of select="format-number((PROFIT div $main_total)*100,'##.##0,00')"/> %
                        </td>
                        <td align="right">
                            <xsl:value-of select="format-number($acum,'##.##0,00')"/>%
                        </td>
                    </tr>
                </xsl:for-each>
                <tr>
                    <td align="right" colspan="4">
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