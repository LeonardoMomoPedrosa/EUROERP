<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:exsl="http://exslt.org/common"
                extension-element-prefixes="exsl"
                version="1.0">
    <xsl:param name="first"/>
    <xsl:param name="last"/>
    <xsl:decimal-format decimal-separator="," grouping-separator="." NaN="0,00"/>
    <xsl:template match="/GROUP_REPORT">
        <br/>
        <div align="left">
            <table border="0" width="100%">
                <xsl:variable name="totalProd" select="sum(NewDataSet/ds[PC_ID=1]/PRICE)"/>
                <xsl:variable name="totalSrvWithCost" select="sum(NewDataSet/ds[PC_ID=2 and HCI='true']/PRICE)"/>
                <xsl:variable name="totalSrvNoCost" select="sum(NewDataSet/ds[PC_ID=2 and HCI='false']/PRICE)"/>
                <xsl:variable name="totalCostProd" select="sum(NewDataSet/ds[PC_ID=1]/CF)"/>
                <xsl:variable name="totalCostSrv" select="sum(NewDataSet/ds[PC_ID=2 and HCI='true']/CF)"/>
                <xsl:variable name="total_credit" select="sum(CreditDs/ds/CREDIT)"/>
                <xsl:variable name="refGroups" select="GroupRefDs/*"/>
                <xsl:call-template name="main-tpl">
                    <xsl:with-param name="groupName">Produtos</xsl:with-param>
                    <xsl:with-param name="refGroups" select="$refGroups[CLASS_ID = 1]"/>
                    <xsl:with-param name="rows" select="NewDataSet/ds[PC_ID = 1]"/>
                    <xsl:with-param name="pcid" select="1"/>
                    <xsl:with-param name="hci" select="1"/>
                    <xsl:with-param name="main_total" select="$totalProd"/>
                    <xsl:with-param name="main_cost" select="$totalCostProd"/>
                </xsl:call-template>
                <tr>
                    <td colspan="3">&#160;</td>
                </tr>
                <xsl:call-template name="main-tpl">
                    <xsl:with-param name="groupName">Serviços com Custo</xsl:with-param>
                    <xsl:with-param name="refGroups" select="$refGroups[CLASS_ID=2]"/>
                    <xsl:with-param name="rows" select="NewDataSet/ds[PC_ID = 2 and HCI='true']"/>
                    <xsl:with-param name="pcid" select="1"/>
                    <xsl:with-param name="hci" select="1"/>
                    <xsl:with-param name="main_total" select="$totalSrvWithCost"/>
                    <xsl:with-param name="main_cost" select="$totalCostSrv"/>
                </xsl:call-template>
                <tr>
                    <td colspan="3">&#160;</td>
                </tr>
                <xsl:call-template name="main-tpl">
                    <xsl:with-param name="groupName">Serviços sem Custo</xsl:with-param>
                    <xsl:with-param name="refGroups" select="$refGroups[CLASS_ID=2]"/>
                    <xsl:with-param name="rows" select="NewDataSet/ds[PC_ID = 2 and HCI='false']"/>
                    <xsl:with-param name="pcid" select="2"/>
                    <xsl:with-param name="hci" select="0"/>
                    <xsl:with-param name="main_total" select="$totalSrvNoCost"/>
                    <xsl:with-param name="main_cost" select="0"/>
                </xsl:call-template>
                <tr>
                    <td colspan="3">&#160;</td>
                </tr>
                <tr>
                    <td bgcolor="yellow" colspan="2">
                        <table border="0">
                            <tr>
                                <td colspan="2">
                                    <b>Resultado no Período</b>
                                </td>
                            </tr>
                            <tr>
                                <td>Receita de Produtos:</td>
                                <td>
                                    R$ <xsl:value-of select="format-number($totalProd,'##.##0,00')"/>
                                </td>
                            </tr>
                            <tr>
                                <td>Receita de Serviços:</td>
                                <td>
                                    R$ <xsl:value-of select="format-number($totalSrvWithCost + $totalSrvNoCost,'##.##0,00')"/>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <b>Receita Total:</b>
                                </td>
                                <td>
                                    <b>
                                        R$ <xsl:value-of select="format-number($totalSrvWithCost + $totalSrvNoCost + $totalProd,'##.##0,00')"/>
                                    </b>
                                </td>
                            </tr>
                            <tr>
                                <td>Custo das Mercadorias:</td>
                                <td>
                                    R$ <xsl:value-of select="format-number($totalCostProd,'##.##0,00')"/>
                                </td>
                            </tr>
                            <tr>
                                <td>Custo dos Serviços:</td>
                                <td>
                                    R$ <xsl:value-of select="format-number($totalCostSrv,'##.##0,00')"/>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <b>Custo Total:</b>
                                </td>
                                <td>
                                    <b>
                                        R$ <xsl:value-of select="format-number($totalCostProd + $totalCostSrv,'##.##0,00')"/>
                                    </b>
                                </td>
                            </tr>
                            <tr>
                                <td>Créditos no Período:</td>
                                <td>
                                    R$ <xsl:value-of select="format-number($total_credit,'##.##0,00')"/>
                                </td>
                            </tr>
                            <tr>
                                <td colspan="2">&#160;</td>
                            </tr>
                            <tr>
                                <td>
                                    <b>Lucro Bruto Total:</b>
                                </td>
                                <td>
                                    <b>
                                        R$ <xsl:value-of select="format-number($totalProd + $totalSrvWithCost + $totalSrvNoCost - $totalCostProd - $totalCostSrv,'##.##0,00')"/> = <xsl:value-of select="format-number((($totalProd + $totalSrvWithCost + $totalSrvNoCost) div ($totalCostProd + $totalCostSrv) - 1)*100,'##.##0,00')"/> %
                                    </b>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </div>
    </xsl:template>

    <xsl:template name="main-tpl">
        <xsl:param name="groupName"/>
        <xsl:param name="refGroups"/>
        <xsl:param name="rows"/>
        <xsl:param name="pcid"/>
        <xsl:param name="hci"/>
        <xsl:param name="main_total"/>
        <xsl:param name="main_cost"/>
        <xsl:variable name="total_class" select="sum($rows/PRICE)"/>
        <xsl:variable name="total_class_qtd" select="sum($rows/QTD)"/>
        <xsl:variable name="total_class_cost" select="sum($rows/CF)"/>
        <xsl:variable name="sortedRefGroups">
            <xsl:for-each select="$refGroups">
                <xsl:sort select="sum($rows[GID=current()/PKId]/PRICE) - sum($rows[GID=current()/PKId]/CF)" data-type="number" order="descending"/>
                <xsl:variable name="total_group" select="sum($rows[GID=current()/PKId]/PRICE)"/>
                <xsl:variable name="total_cost_group" select="sum($rows[GID=current()/PKId]/CF)"/>
                <xsl:variable name="totalQtd" select="sum($rows[GID=current()/PKId]/QTD)"/>
                <ITEM>
                    <PKId>
                        <xsl:value-of select="PKId"/>
                    </PKId>
                    <NAME>
                        <xsl:value-of select="NAME"/>
                    </NAME>
                    <TOTAL>
                        <xsl:value-of select="$total_group"/>
                    </TOTAL>
                    <TOTAL_COST>
                        <xsl:value-of select="$total_cost_group"/>
                    </TOTAL_COST>
                    <PROFIT>
                        <xsl:value-of select="$total_group - $total_cost_group"/>
                    </PROFIT>
                    <QTD>
                        <xsl:value-of select="$totalQtd"/>
                    </QTD>
                </ITEM>
            </xsl:for-each>
        </xsl:variable>
        <tr bgcolor="#E3E5A3">
            <td align="left" colspan="2">
                <B>
                    <xsl:value-of select="$groupName"/>
                </B>
            </td>
            <td align="right">
                Qtd Venda
            </td>
            <td align="right">
                Total Venda R$
            </td>
            <td align="right">
                Custo Venda R$
            </td>
            <td align="right">
                Lucro %
            </td>
            <td align="right">
                Lucro (R$)
            </td>
            <td align="right">
                Peso
            </td>
            <td align="right">
                Acum. %
            </td>
        </tr>
        <xsl:for-each select="exsl:node-set($sortedRefGroups)/ITEM">
            <xsl:variable name="total_group" select="TOTAL"/>
            <xsl:variable name="total_cost_group" select="TOTAL_COST"/>
            <xsl:variable name="total_profit" select="sum(exsl:node-set($sortedRefGroups)/ITEM/PROFIT)"/>
            <xsl:variable name="currPos" select="position()"/>
            <xsl:variable name="preAcum" select="sum(exsl:node-set($sortedRefGroups)/ITEM[position() &lt; $currPos]/PROFIT) div $total_profit * 100"/>
            <xsl:variable name="acum" select="$preAcum + ($total_group - $total_cost_group) div $total_profit * 100"/>
            <tr>
                <xsl:if test="(position() mod 2 = 1)">
                    <xsl:attribute name="bgcolor">#CDCDCD</xsl:attribute>
                </xsl:if>
                <td width="3px">
                    <xsl:if test="$hci = 1">
                        <xsl:attribute name="bgcolor">
                            <xsl:choose>
                                <xsl:when test="$preAcum &gt;= 95">orange</xsl:when>
                                <xsl:when test="$preAcum &gt;= 80">green</xsl:when>
                                <xsl:otherwise>blue</xsl:otherwise>
                            </xsl:choose>
                        </xsl:attribute>
                    </xsl:if>
                </td>
                <td align="left">
                    <a href="#">
                        <xsl:attribute name="onClick">
                            window.open('/vendas/relatorios/abc/grupo?groupId=<xsl:value-of select="PKId"/>&amp;groupName=<xsl:value-of select="NAME"/>&amp;first=<xsl:value-of select="$first"/>&amp;last=<xsl:value-of select="$last"/>','','height=600,width=1000,left=50,top=50,scrollbars=1,menubar=0,resizable');return false;
                        </xsl:attribute>
                        <xsl:value-of select="NAME"/>
                    </a>
                </td>
                <td align="right">
                    <xsl:value-of select="format-number(QTD,'##.##0,00')"/>
                </td>
                <td align="right">
                    <xsl:value-of select="format-number($total_group,'##.##0,00')"/>
                </td>
                <td align="right">
                    <xsl:if test="$hci = 1">
                        <xsl:value-of select="format-number($total_cost_group,'##.##0,00')"/>
                    </xsl:if>
                </td>
                <td align="right">
                    <xsl:variable name="profitPerc" select="($total_group div ($total_cost_group) - 1)*100"/>
                    <xsl:if test="$hci = 1">
                        <font size="1">
                            <xsl:attribute name="color">
                                <xsl:choose>
                                    <xsl:when test="$profitPerc &gt;= 0">blue</xsl:when>
                                    <xsl:otherwise>red</xsl:otherwise>
                                </xsl:choose>
                            </xsl:attribute>
                            <xsl:value-of select="format-number($profitPerc,'##.##0,00')"/> %
                        </font>
                    </xsl:if>
                </td>
                <td align="right">
                    <xsl:if test="$hci = 1">
                        <font>
                            <xsl:attribute name="color">
                                <xsl:choose>
                                    <xsl:when test="PROFIT &gt;= 0">blue</xsl:when>
                                    <xsl:otherwise>red</xsl:otherwise>
                                </xsl:choose>
                            </xsl:attribute>
                            <b>
                                <xsl:value-of select="format-number(PROFIT,'##.##0,00')"/>
                            </b>
                        </font>
                    </xsl:if>
                </td>
                <td align="right">
                    <font size="1">
                        <xsl:if test="$hci = 1">
                            <xsl:value-of select="format-number((($total_group - $total_cost_group) div ($total_class - $total_class_cost))*100,'##.##0,00')"/>%
                        </xsl:if>
                    </font>
                </td>
                <td align="right">
                    <font size="1">
                        <xsl:if test="$hci = 1">
                            <xsl:value-of select="format-number($acum,'##.##0,00')"/>%
                        </xsl:if>
                    </font>
                </td>
            </tr>
        </xsl:for-each>
        <tr>
            <td align="right" colspan="2">
                <b>Total</b>
            </td>
            <td align="right">
                <b>
                    <xsl:value-of select="format-number($total_class_qtd,'##.##0,00')"/>
                </b>
            </td>
            <td align="right">
                <b>
                    R$ <xsl:value-of select="format-number($total_class,'##.##0,00')"/>
                </b>
            </td>
            <td align="right">
                <b>
                    <xsl:if test="$hci = 1">
                        R$ <xsl:value-of select="format-number($total_class_cost,'##.##0,00')"/>
                    </xsl:if>
                </b>
            </td>
            <td align="right">
                <xsl:variable name="totalProfitPerc" select="($total_class div ($total_class_cost) - 1)*100"/>
                <xsl:if test="$hci = 1">
                    <font>
                        <xsl:attribute name="color">
                            <xsl:choose>
                                <xsl:when test="$totalProfitPerc &gt;= 0">blue</xsl:when>
                                <xsl:otherwise>red</xsl:otherwise>
                            </xsl:choose>
                        </xsl:attribute>
                        <b>
                            <xsl:value-of select="format-number($totalProfitPerc,'##.##0,00')"/> %
                        </b>
                    </font>
                </xsl:if>
            </td>
            <td align="right">
                <xsl:variable name="totalProfit" select="$total_class - $total_class_cost"/>
                <font>
                    <xsl:if test="$hci = 1">
                        <xsl:attribute name="color">
                            <xsl:choose>
                                <xsl:when test="$totalProfit &gt;= 0">blue</xsl:when>
                                <xsl:otherwise>red</xsl:otherwise>
                            </xsl:choose>
                        </xsl:attribute>
                        <b>
                            R$ <xsl:value-of select="format-number($totalProfit,'##.##0,00')"/>
                        </b>
                    </xsl:if>
                </font>
            </td>
            <td align="right">
                <b>
                    <xsl:if test="$hci = 1">
                        <xsl:value-of select="format-number((($total_class - $total_class_cost) div ($main_total - $main_cost))*100,'##.##0,00')"/> %
                    </xsl:if>
                </b>
            </td>
        </tr>
    </xsl:template>
</xsl:stylesheet>

