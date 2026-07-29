<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:exsl="http://exslt.org/common"
                extension-element-prefixes="exsl"
                version="1.0">
    <xsl:param name="groupId"/>
    <xsl:param name="first"/>
    <xsl:param name="last"/>
    <xsl:decimal-format decimal-separator="," grouping-separator="."/>
    <xsl:key name="rows_pid" match="ds" use="PID" />
    <xsl:template match="/GROUP_REPORT">
        <br/>
        <div align="left">
            <table border="0" width="100%">
                <tr bgcolor="#E3E5A3">
                    <td align="right">
                    </td>
                    <td align="right">
                        <font size="1">
                            <b>Cod.</b>
                        </font>
                    </td>
                    <td align="left">
                        <font size="1">
                            <b>PRODUTO</b>
                        </font>
                    </td>
                    <td align="right">
                        <font size="1">
                            <b>
                                Qtd.Venda
                            </b>
                        </font>
                    </td>
                    <td align="right">
                        <font size="1">
                            <b>Tot. Venda R$</b>
                        </font>
                    </td>
                    <td align="right">
                        <font size="1">
                            <b>Custo Venda R$</b>
                        </font>
                    </td>
                    <td align="right">
                        <font size="1">
                            <b>Lucro (%)</b>
                        </font>
                    </td>
                    <td align="right">
                        <font size="1">
                            <b>Lucro (R$)</b>
                        </font>
                    </td>
                    <td align="right">
                        <font size="1">
                            <b>Peso</b>
                        </font>
                    </td>
                    <td align="right">
                        <font size="1">
                            <b>Acum. %</b>
                        </font>
                    </td>
                </tr>
                <xsl:variable name="total" select="sum(NewDataSet/ds[GID=$groupId]/PRICE)"/>
                <xsl:variable name="total_cost" select="sum(NewDataSet/ds[GID=$groupId]/CF)"/>
                <xsl:variable name="rows" select="NewDataSet/ds[GID=$groupId]"/>
                <xsl:variable name="sortedProducts">
                    <xsl:for-each select="$rows[count(. | key('rows_pid', PID)[1]) = 1]">
                        <xsl:sort select="sum($rows[PID=current()/PID]/PRICE) - sum($rows[PID=current()/PID]/CF)" data-type="number" order="descending"/>
                        <xsl:variable name="total_product" select="sum($rows[PID=current()/PID]/PRICE)"/>
                        <xsl:variable name="total_qtd_i" select="sum($rows[PID=current()/PID]/QTD)"/>
                        <xsl:variable name="total_cost_product" select="sum($rows[PID=current()/PID]/CF)"/>
                        <ITEM>
                            <MIND>
                                0
                            </MIND>
                            <PKId>
                                <xsl:value-of select="PID"/>
                            </PKId>
                            <QTD>
                                <xsl:value-of select="$total_qtd_i"/>
                            </QTD>
                            <NAME>
                                <xsl:value-of select="NAME"/>&#160;<xsl:value-of select="AZ"/>
                            </NAME>
                            <TOTAL>
                                <xsl:value-of select="$total_product"/>
                            </TOTAL>
                            <TOTAL_COST>
                                <xsl:value-of select="$total_cost_product"/>
                            </TOTAL_COST>
                            <PROFIT>
                                <xsl:value-of select="$total_product - $total_cost_product"/>
                            </PROFIT>
                        </ITEM>
                    </xsl:for-each>
                </xsl:variable>
                <xsl:for-each select="exsl:node-set($sortedProducts)/ITEM">
                    <xsl:variable name="total_product" select="TOTAL"/>
                    <xsl:variable name="total_cost_product" select="TOTAL_COST"/>
                    <xsl:variable name="total_profit" select="sum(exsl:node-set($sortedProducts)/ITEM/PROFIT)"/>
                    <xsl:variable name="currPos" select="position()"/>
                    <xsl:variable name="preAcum" select="sum(exsl:node-set($sortedProducts)/ITEM[position() &lt; $currPos]/PROFIT) div $total_profit * 100"/>
                    <xsl:variable name="acum" select="$preAcum + ($total_product - $total_cost_product) div ($total - $total_cost) * 100"/>
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
                        <td align="left">
                            <a href="#">
                                <xsl:attribute name="onClick">
                                    window.open('/vendas/relatorios/abc/grupo/pedidos?productId=<xsl:value-of select="PKId"/>&amp;productName=<xsl:value-of select="NAME"/>&amp;first=<xsl:value-of select="$first"/>&amp;last=<xsl:value-of select="$last"/>','','height=350,width=600,left=250,top=150,scrollbars=1,menubar=0');return false;
                                </xsl:attribute>
                                <xsl:value-of select="NAME"/>
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
                            <font>
                                <xsl:variable name="profitPerc" select="(($total_product - $total_cost_product) div ($total_cost_product))*100"/>
                                <xsl:attribute name="color">
                                    <xsl:choose>
                                        <xsl:when test="$profitPerc &gt;= 0">blue</xsl:when>
                                        <xsl:otherwise>red</xsl:otherwise>
                                    </xsl:choose>
                                </xsl:attribute>
                                <xsl:value-of select="format-number($profitPerc,'##.##0')"/> %
                            </font>
                        </td>
                        <td align="right">
                            <xsl:variable name="profit" select="$total_product - $total_cost_product"/>
                            <font>
                                <xsl:attribute name="color">
                                    <xsl:choose>
                                        <xsl:when test="$profit &gt;= 0">blue</xsl:when>
                                        <xsl:otherwise>red</xsl:otherwise>
                                    </xsl:choose>
                                </xsl:attribute>
                                <b>
                                    <xsl:value-of select="format-number($profit,'##.##0,00')"/>
                                </b>
                            </font>
                        </td>
                        <td align="right">
                            <xsl:value-of select="format-number((($total_product - $total_cost_product) div ($total - $total_cost))*100,'##.##0,00')"/>%
                        </td>
                        <td align="right">
                            <xsl:value-of select="format-number($acum,'##.##0,00')"/>%
                        </td>
                    </tr>
                </xsl:for-each>
                <xsl:variable name="total_qtd" select="sum(exsl:node-set($sortedProducts)/ITEM/QTD)"/>
                <tr>
                    <td align="right" colspan="3">
                        <b>Total</b>
                    </td>
                    <td align="right">
                        <b>
                            <xsl:value-of select="$total_qtd"/>
                        </b>
                    </td>
                    <td align="right">
                        <b>
                            R$ <xsl:value-of select="format-number($total,'##.##0,00')"/>
                        </b>
                    </td>
                    <td align="right">
                        <b>
                            R$ <xsl:value-of select="format-number($total_cost,'##.##0,00')"/>
                        </b>
                    </td>
                    <td align="right">
                        <xsl:variable name="totProfitPerc" select="(($total - $total_cost) div ($total_cost))*100"/>
                        <font>
                            <xsl:attribute name="color">
                                <xsl:choose>
                                    <xsl:when test="$totProfitPerc &gt;= 0">blue</xsl:when>
                                    <xsl:otherwise>red</xsl:otherwise>
                                </xsl:choose>
                            </xsl:attribute>
                            <b>
                                <xsl:value-of select="format-number($totProfitPerc,'##.##0')"/> %
                            </b>
                        </font>
                    </td>
                    <td align="right">
                        <xsl:variable name="totProfit" select="$total - $total_cost"/>
                        <font>
                            <xsl:attribute name="color">
                                <xsl:choose>
                                    <xsl:when test="$totProfit &gt;= 0">blue</xsl:when>
                                    <xsl:otherwise>red</xsl:otherwise>
                                </xsl:choose>
                            </xsl:attribute>
                            <b>
                                R$ <xsl:value-of select="format-number($totProfit,'##.##0,00')"/>
                            </b>
                        </font>
                    </td>
                    <td></td>
                </tr>
            </table>
        </div>
    </xsl:template>
</xsl:stylesheet>