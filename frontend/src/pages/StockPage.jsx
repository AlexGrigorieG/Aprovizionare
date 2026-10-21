import { useApi } from '../api'
import { Empty, ErrorBox, Loading, PageHeader } from '../components/ui'
import { formatQuantity } from '../format'

export default function StockPage() {
  const { data: stock, error, loading } = useApi('/stock')

  return (
    <section className="page">
      <PageHeader title="Stoc" subtitle="Calculat din toate recepțiile, inclusiv marfa de clarificat. Ieșirile din stoc nu fac parte din aplicație." />
      <ErrorBox error={error} />

      {loading ? (
        <Loading />
      ) : !stock?.length ? (
        <Empty>Nu există produse.</Empty>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Cod</th>
                <th>Produs</th>
                <th className="num">Cantitate</th>
              </tr>
            </thead>
            <tbody>
              {stock.map((item) => (
                <tr key={item.productId} className={item.quantity === 0 ? 'disabled' : ''}>
                  <td className="mono">{item.code}</td>
                  <td>{item.name}</td>
                  <td className="num strong">{formatQuantity(item.quantity)} {item.unit}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
